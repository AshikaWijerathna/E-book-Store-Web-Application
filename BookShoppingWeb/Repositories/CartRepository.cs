using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BookShoppingWeb.Repositories
{
    public class CartRepository:ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartRepository(ApplicationDbContext db,IHttpContextAccessor httpContextAccessor,UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<int>AddItem(int bookId, int qty)
        {
            string userId = GetUserId();
            using var transaction = await _db.Database.BeginTransactionAsync();
            ShoppingCart cart = null;
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedAccessException("User is not Logged In");
                }
                cart = await GetCart(userId);
                if (cart is null)
                {
                    cart = new ShoppingCart
                    {
                        UserId = userId
                    };
                    _db.ShoppingCarts.Add(cart);

                }
                await _db.SaveChangesAsync();
                //Cart details section
                var cartItem = _db.CartDetails.FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if (cartItem is not null)
                {
                    cartItem.Quantity += qty;
                }
                else
                {
                    var book = _db.Books.Find(bookId);
                    cartItem = new CartDetail
                    {
                        BookId = bookId,
                        ShoppingCartId = cart.Id,
                        Quantity = qty,
                        UnitPrice=book.Price //new line after jupdating the database
                    };
                    _db.CartDetails.Add(cartItem);
                }
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }
        public async Task<int> RemoveItem(int bookId)
        {
            //using var transaction = await _db.Database.BeginTransactionAsync();
            string userId = GetUserId();
            ShoppingCart cart = null;
            try
            {
                
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedAccessException("User is not logged-in");
                }
                cart = await GetCart(userId);
                if (cart is null)
                {
                    throw new InvalidOperationException("Invalid Cart");

                }

                _db.SaveChanges();
                //Cart details section
                var cartItem = _db.CartDetails.FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);

                if (cartItem is null)
                {
                    throw new Exception("No items in cart");
                }

                else if (cartItem.Quantity == 1)
                {
                    _db.CartDetails.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = cartItem.Quantity - 1;
                }
                _db.SaveChanges();
                //transaction.Commit()
            }
            catch (Exception ex)
            {
          
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }
        public async Task<ShoppingCart> GetUserCart()
        {
            var userId = GetUserId();
            if (userId == null)
                throw new InvalidOperationException("Invalid userid");
            var shoppingCart = await _db.ShoppingCarts
                               .Include(a => a.CartDetails)
                               .ThenInclude(a => a.Book)
                               .ThenInclude(a=> a.Stock)
                               .Include(a=>a.CartDetails)
                               .ThenInclude(a=>a.Book)
                               .ThenInclude(a => a.Genre)
                               .Where(a => a.UserId == userId).FirstOrDefaultAsync();
            return shoppingCart;
                               
        }
        public async Task<ShoppingCart> GetCart(string userId)
        {
            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);
            return cart;
        }
      
        public async Task<int> GetCartItemCount(string userId = "")
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = GetUserId();
            }

            return await _db.CartDetails
                .Where(cd => cd.ShoppingCart.UserId == userId)
                .SumAsync(cd => (int?)cd.Quantity) ?? 0;
        }
        public async Task<bool> DoCheckout(CheckoutModel model)
        {
            try
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                //Logic ===> move data from cartDetail to order detail then we will remove cart detail.
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedAccessException("User is not logged In");
                }
                var cart = await GetCart(userId);
                if(cart is null)
                {
                    throw new InvalidOperationException("Invalid Cart");
                }
                var cartDetail = _db.CartDetails.Where(a => a.ShoppingCartId == cart.Id).ToList();
                if(cartDetail.Count == 0)
                {
                    throw new InvalidOperationException("Cart is Empty");
                }
                var pendingRecord = _db.OrderStatuses.FirstOrDefault(s => s.StatusName == "Pending");
                if(pendingRecord is null)
                {
                    throw new InvalidOperationException("Order status doesn't have Pending status");
                }
                var order = new Order
                {
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    Name=model.Name,
                    Email=model.Email,
                    MobileNumber=model.MobileNumber,
                    PaymentMeythod=model.PaymentMethod,
                    Address=model.Address,
                    IsPaid =false,
                    OrderStatusId = pendingRecord.Id,//pending
                    
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                foreach(var item in cartDetail)
                {
                    var orderDetail = new OrderDetail
                    {
                        BookId = item.BookId,
                        OrderId = order.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _db.OrderDetails.Add(orderDetail);
                    //update Stock
                    var stock = await _db.Stocks.FirstOrDefaultAsync(a => a.BookId == item.BookId);
                    if(stock == null)
                    {
                        throw new InvalidOperationException("Stock is null");
                    }
                    if(item.Quantity > stock.Quantity)
                    {
                        throw new InvalidOperationException($"Only {stock.Quantity} items(s) are available in the stock");
                    }
                    stock.Quantity -= item.Quantity;
                }
                _db.SaveChanges();

                //removing the cart details
                _db.CartDetails.RemoveRange(cartDetail);
                _db.SaveChanges();
                transaction.Commit();
                return true;
             }
            catch(Exception)
            {
                return false;
            }
        }
        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }
        //Cart Clearance
        public async Task ClearCart()
        {
            var cart = await GetUserCart();
            if(cart !=null && cart.CartDetails.Any())
            {
                _db.CartDetails.RemoveRange(cart.CartDetails);
                await _db.SaveChangesAsync();
            }
        }
    }
}
