namespace BookShoppingWeb.Models
{
    public class DashboardStats
    {
        public double TodaySales { get; set; }
        public double MonthlySales { get; set; }
        public double YearlySales { get; set; }
        public double TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int PendingCOD { get; set; }
        public int TotalBooksSold { get; set; }
    }
}
