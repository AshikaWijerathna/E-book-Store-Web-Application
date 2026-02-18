using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
//Newly Added code
using Stripe.Checkout;
using Stripe;
using BookShoppingWeb.Models;

namespace BookShoppingWeb.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepo;
        private readonly ApplicationDbContext _context;

        public CartController(ICartRepository cartRepo, ApplicationDbContext context)
        {
            _cartRepo = cartRepo;
            _context = context;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}
        public async Task<IActionResult> AddItem(int bookId, int qty = 1, int redirect = 0)
        {
            var cartCount = await _cartRepo.AddItem(bookId, qty);
            if (redirect == 0)
            {
                return Ok(cartCount);
            }
            return RedirectToAction("GetUserCart");
        }
        public async Task<IActionResult> RemoveItem(int bookId)
        {
            var cartCount = await _cartRepo.RemoveItem(bookId);
            return RedirectToAction("GetUserCart");
        }
        public async Task<IActionResult> GetUserCart()
        {
            var cart = await _cartRepo.GetUserCart();
            return View(cart);
        }
        public async Task<IActionResult> GetTotalItemInCart()
        {
            int cartItem = await _cartRepo.GetCartItemCount();
            return Ok(cartItem);
        }
        public IActionResult Checkout()
        {
            return View();
        }
        public IActionResult OrderSuccess()
        {
            return View();
        }
        public IActionResult OrderFailure()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            if (string.Equals(model.PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
            {
                bool ok = await _cartRepo.DoCheckout(model);
                if (!ok)
                {
                    return RedirectToAction(nameof(OrderFailure));
                }
                return RedirectToAction(nameof(OrderSuccess));
            }

            // ================= ONLINE PAYMENT =================

            var cart = await _cartRepo.GetUserCart();

            if (cart == null || !cart.CartDetails.Any())
                return RedirectToAction("GetUserCart");

            // create order (without clearing cart)
            var pendingStatus = _context.OrderStatuses
                                .First(s => s.StatusName == "Pending");

            var order = new Order
            {
                UserId = User.Identity.Name,
                Name = model.Name,
                Email = model.Email,
                MobileNumber = model.MobileNumber,
                Address = model.Address,
                PaymentMeythod = model.PaymentMethod,
                OrderStatusId = pendingStatus.Id,
                IsPaid = false
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart.CartDetails)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.Id,
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            await _context.SaveChangesAsync();

            // ===== STRIPE CHECKOUT =====

            var domain = "https://localhost:7031/";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = domain + $"Payment/Success?orderId={order.Id}",
                CancelUrl = domain + "Payment/Cancel",
                LineItems = new List<SessionLineItemOptions>()
            };

            foreach (var item in cart.CartDetails)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    Quantity = item.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(item.UnitPrice * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Book.BookName
                        }
                    }
                });
            }

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }
    }
}
