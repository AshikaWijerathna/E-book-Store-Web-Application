using BookShoppingWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingWeb.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartRepository _cartRepo;
        private readonly EmailService _emailService;

        public PaymentController(ApplicationDbContext context, ICartRepository cartRepo, EmailService emailService)
        {
            _context = context;
            _cartRepo = cartRepo;
            _emailService = emailService;
        }
        public async Task<ActionResult> Success(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if(order!=null && !order.IsPaid)
            {
                order.IsPaid = true;

                var paidStatus = _context.OrderStatuses.FirstOrDefault(s => s.StatusName == "Paid");
                if(paidStatus != null)
                {
                    order.OrderStatusId = paidStatus.Id;
                }
                await _context.SaveChangesAsync();
                //Get Order Items
                var items = _context.OrderDetails.Where(x => x.OrderId == orderId).ToList();
                //Reduce the stock count
                foreach(var item in items)
                {
                    var stock = _context.Stocks.FirstOrDefault(s => s.BookId == item.BookId);
                    if(stock != null)
                    {
                        stock.Quantity -= item.Quantity;
                        //prevent negative stock
                        if(stock.Quantity < 0)
                        {
                            stock.Quantity = 0;
                        }
                    }
                }
                await _context.SaveChangesAsync();
                double total = items.Sum(i => i.UnitPrice * i.Quantity);

                //Email body
                string body = $@"
                     <h2>Thank you for your purchase!</h2>
                     <p>Your payment was successful.</p>
                     <p><strong>Order ID:</strong>{order.Id}</p>
                     <p><strong>Total Amount:</strong>${total}</p>
                     <p><strong>Delivery Address:</strong>{order.Address}</p>
                     <br/>
                     <p>We will deliver your books soon 📚</p>
                     ";
                await _emailService.SendEmailAsync(
                    order.Email,
                    "Payment Receipt - Book Shopping Web Store",
                    body
                );

            }
            await _cartRepo.ClearCart();
            return View("~/Views/Cart/PaymentSuccess.cshtml");
        }
        public IActionResult Cancel()
        {
            return View("~/Views/Cart/OrderFailure.cshtml");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
