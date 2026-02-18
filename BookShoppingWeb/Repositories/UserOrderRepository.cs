using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingWeb.Repositories
{
    public class UserOrderRepository: IUserOrderRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        public UserOrderRepository(ApplicationDbContext db,UserManager<IdentityUser> userManager,IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task ChangeOrderStatus(UpdateOrderStatusModel data)
        {
            var order = await _db.Orders.FindAsync(data.OrderId);
            if(order == null)
            {
                throw new InvalidOperationException($"order with id:{data.OrderId} does not found");
            }
            order.OrderStatusId = data.OrderStatusId;
            await _db.SaveChangesAsync();
        }

        public  async Task<Order?> GetOrderById(int id)
        {
            return await _db.Orders.FindAsync(id);
        }

        public async Task<IEnumerable<OrderStatus>> GetOrderStatuses()
        {
            return await _db.OrderStatuses.ToListAsync();
        }

        public async Task TogglePaymentStatus(int orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null)
            {
                throw new InvalidOperationException($"order with id:{orderId} does not found");
            }
            order.IsPaid = !order.IsPaid;
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Order>> UserOrders(bool getAll = false)
        {

            var orders = _db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetail)
                .ThenInclude(od => od.Book)
                .ThenInclude(b => b.Genre).AsQueryable();
                     
            if (!getAll)
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("User is not logged in");
                }
                orders = orders.Where(a => a.UserId == userId);
                //return await orders.ToListAsync();
            }
            return await orders.ToListAsync();
        }
        public async Task<IEnumerable<Order>> GetOrdersByPaymentMethod(string method)
        {
            var userId = GetUserId();

            return await _db.Orders
                .Where(o => o.UserId == userId && o.PaymentMeythod == method)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetail)
                .ThenInclude(od => od.Book)
                .ThenInclude(b => b.Genre)
                .OrderByDescending(o => o.Id).ToListAsync();
        }
        public async Task<Order?> GetOrderWithDetails(int orderId)
        {
            return await _db.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetail)
                .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }
        public async Task<DashboardStats> GetDashboardStats()
        {
            var orders = await _db.Orders
                .Include(o => o.OrderDetail)
                .ToListAsync();

            var paidOrders = orders.Where
                (o => o.IsPaid).ToList();

            DateTime today = DateTime.Today;
            DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            DateTime firstDayOfYear = new DateTime(today.Year, 1, 1);

            double totalSales = paidOrders
                .SelectMany(o => o.OrderDetail)
                .Sum(d => d.UnitPrice * d.Quantity);

            double todaySales = paidOrders
                .Where(o => o.CreateDate.Date == today)
                .SelectMany(o => o.OrderDetail)
                .Sum(d => d.UnitPrice * d.Quantity);

            double monthlySales = paidOrders
                .Where(o => o.CreateDate >= firstDayOfMonth)
                .SelectMany(o => o.OrderDetail)
                .Sum(d => d.UnitPrice * d.Quantity);

            double yearlySales = paidOrders
                .Where(o => o.CreateDate >= firstDayOfYear)
                .SelectMany(o => o.OrderDetail)
                .Sum(d => d.UnitPrice * d.Quantity);

            int totalBookSold = paidOrders
                .SelectMany(o => o.OrderDetail)
                .Sum(d => d.Quantity);

            return new DashboardStats
            {
                TodaySales = todaySales,
                MonthlySales = monthlySales,
                YearlySales=yearlySales,
                TotalSales = totalSales,
                TotalOrders = orders.Count,
                PaidOrders = paidOrders.Count,
                PendingCOD = orders.Count(o => o.PaymentMeythod == "COD" && !o.IsPaid),
                TotalBooksSold = totalBookSold
            };
        }
        public async Task<DashboardCharts> GetDashboardCharts()
        {
            var paidOrders = await _db.Orders
                .Where(o => o.IsPaid)
                .Include(o => o.OrderDetail)
                .ThenInclude(d => d.Book)
                .ThenInclude(b=>b.Genre)
                .ToListAsync();

            var charts = new DashboardCharts();

            //Daily Sales(Last 7 days)
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);

                var total = paidOrders
                    .Where(o => o.CreateDate.Date == date)
                    .SelectMany(o => o.OrderDetail)
                    .Sum(d => d.UnitPrice * d.Quantity);

                charts.DailyLabels.Add(date.ToString("dd MMM"));
                charts.DailySales.Add(total);
            }

            //Monthly Sales(last 6months)
            for(int i=5; i>=0; i--)
            {
                var month = DateTime.Today.AddMonths(-i);

                var total = paidOrders
                    .Where(o => o.CreateDate.Month == month.Month && o.CreateDate.Year == month.Year)
                    .SelectMany(o => o.OrderDetail)
                    .Sum(d => d.UnitPrice * d.Quantity);

                charts.MonthlyLabels.Add(month.ToString("MMM"));
                charts.MonthlySales.Add(total);
            }
            //Top Selling Books
            var topBooks = paidOrders
                .SelectMany(o => o.OrderDetail)
                .GroupBy(d => d.Book.BookName)
                .Select(g => new
                {
                    Book = g.Key,
                    Qty = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToList();

            charts.TopBooks = topBooks.Select(x => x.Book).ToList();
            charts.TopBookSales = topBooks.Select(x => x.Qty).ToList();

            //Top Selling Genre
            var genreSales = paidOrders
                .SelectMany(o => o.OrderDetail)
                .Where(d=>d.Book !=null)
                .GroupBy(d => d.Book.Genre !=null ? d.Book.Genre.GenreName : "Unknown")
                .Select(g => new
                {
                    Genre = g.Key,
                    Qty = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Qty)
                .Take(5).ToList();

            charts.GenreLabels = genreSales.Select(x => x.Genre).ToList();
            charts.GenreSales = genreSales.Select(x => x.Qty).ToList();

            var topCustomers = paidOrders
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    Customer = g.Key,
                    Orders = g.Count()
                })
                .OrderByDescending(x => x.Orders)
                .Take(5).ToList();

            charts.TopCustomers = topCustomers.Select(x => x.Customer).ToList();
            charts.CustomerOrders = topCustomers.Select(x => x.Orders).ToList();

            return charts;
        }
        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }
    }
}
