namespace BookShoppingWeb.Models
{
    public class DashboardCharts
    {
        public List<string> DailyLabels { get; set; } = new();
        public List<double> DailySales { get; set; } = new();

        public List<string> MonthlyLabels { get; set; } = new();
        public List<double> MonthlySales { get; set; } = new();

        public List<string> TopBooks { get; set; } = new();
        public List<int> TopBookSales { get; set; } = new();

        public List<string> GenreLabels { get; set; } = new();
        public List<int> GenreSales { get; set; } = new();

        public List<string> TopCustomers { get; set; } = new();
        public List<int> CustomerOrders { get; set; } = new();
    }
}
