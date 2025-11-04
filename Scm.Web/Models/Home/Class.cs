using Scm.Domain.Entities;

namespace Scm.Web.Models.Home
{
    public class HomeIndexViewModel
    {
        public int NewOrdersCount { get; set; }
        public int InProgressCount { get; set; }
        public int PartsInStockCount { get; set; }
        public int OverdueCount { get; set; }
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
    }

    public class RecentOrderViewModel
    {
        public Guid Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public OrderPriority Priority { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? SLAUntil { get; set; }
    }
}