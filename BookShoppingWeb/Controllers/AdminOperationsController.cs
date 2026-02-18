using BookShoppingWeb.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookShoppingWeb.Controllers
{
    [Authorize(Roles =nameof(Roles.Admin))]
    public class AdminOperationsController : Controller
    {
        private readonly IUserOrderRepository _userOrderRepository;
        private readonly IStockRepository _stockRepository;

        public AdminOperationsController(IUserOrderRepository userOrderRepository,IStockRepository stockRepository)
        {
            _userOrderRepository = userOrderRepository;
            _stockRepository = stockRepository;
        }
        public async Task<IActionResult> AllOrders()
        {
            var orders = await _userOrderRepository.UserOrders(true);
            return View(orders);
        }
        public async Task<IActionResult> TogglePaymentStatus(int orderId)
        {
            try
            {
                await _userOrderRepository.TogglePaymentStatus(orderId);
            }catch(Exception ex)
            {
                //log Exception here
            }
            return RedirectToAction(nameof(AllOrders));
        }
        public async Task<IActionResult> UpdateOrderStatus(int orderId)
        {
            var order = await _userOrderRepository.GetOrderById(orderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order with id: {orderId} doesn't found.");
            }
            var orderStatusList = (await _userOrderRepository.GetOrderStatuses()).Select(orderStatus =>
            {
                return new SelectListItem
                {
                    Value = orderStatus.Id.ToString(),
                    Text = orderStatus.StatusName,
                    Selected=order.OrderStatusId == orderStatus.Id
                };
            });
            var data = new UpdateOrderStatusModel
            {
                OrderId = orderId,
                OrderStatusId = order.OrderStatusId,
                OrderStatusList = orderStatusList
            };
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusModel data)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    data.OrderStatusList = (await _userOrderRepository.GetOrderStatuses()).Select(
                        (orderStatus =>
                        {
                            return new SelectListItem
                            {
                                Value = orderStatus.Id.ToString(),
                                Text = orderStatus.StatusName,
                                Selected = orderStatus.Id == data.OrderStatusId };
                        }));
                    return View(data);
                }
                await _userOrderRepository.ChangeOrderStatus(data);
                TempData["msg"] = "Updated successfully";
            }
            catch (Exception ex)
            {
                TempData["msg"] = "Something went Wrong";
            }
            return RedirectToAction(nameof(UpdateOrderStatus), new
            {orderId = data.OrderId});

        }
        [Authorize(Roles=nameof(Roles.Admin))]
        //public IActionResult Dashboard()
        //{
        //    return View();
        //}
        public async Task<IActionResult> Dashboard()
        {
            var stats = await _userOrderRepository.GetDashboardStats();
            var charts = await _userOrderRepository.GetDashboardCharts();
            var lowStock = await _stockRepository.GetLowStockItems(5);

            ViewBag.Charts = charts;
            ViewBag.LowStock = lowStock;
            return View(stats);
        }
    }
}
