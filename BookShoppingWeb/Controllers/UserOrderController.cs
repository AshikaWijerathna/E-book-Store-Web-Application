using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingWeb.Controllers
{
    [Authorize]
    public class UserOrderController : Controller
    {
        private readonly IUserOrderRepository _userOrderRepo;

        public UserOrderController(IUserOrderRepository userOrderPage)
        {
            _userOrderRepo = userOrderPage;
        }

        public async Task<IActionResult> UserOrders()
        {
            var orders = await _userOrderRepo.UserOrders();
            return View(orders);
        }
        //For COD Users.
        public async Task<IActionResult> CodeOrders()
        {
            var orders = await _userOrderRepo.GetOrdersByPaymentMethod("COD");
            return View("UserOrders", orders);
        }
        //For Online Users
        public async Task<IActionResult> OnlineOrders()
        {
            var orders = await _userOrderRepo.GetOrdersByPaymentMethod("Online");
            return View("UserOrders", orders);
        }
        public async Task<IActionResult> TrackOrder(int orderId)
        {
            var orders = await _userOrderRepo.UserOrders();
            var order = orders.FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

    }
    
}
