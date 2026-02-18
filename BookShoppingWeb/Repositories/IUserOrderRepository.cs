namespace BookShoppingWeb.Repositories
{
    public interface IUserOrderRepository
    {
        Task<IEnumerable<Order>> UserOrders(bool getAll=false);
        Task ChangeOrderStatus(UpdateOrderStatusModel data);
        Task TogglePaymentStatus(int orderId);
        Task<Order?> GetOrderById(int id);
        Task<IEnumerable<OrderStatus>> GetOrderStatuses();
        Task<IEnumerable<Order>> GetOrdersByPaymentMethod(string method);
        Task<Order?> GetOrderWithDetails(int orderId);
        Task<DashboardStats> GetDashboardStats();
        Task<DashboardCharts> GetDashboardCharts();
        
    }
}