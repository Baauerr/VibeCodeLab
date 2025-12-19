using System;
namespace Mockups.Models.Analytics
{
    public class AnalyticsSummaryViewModel
    {
        public int UsersCount { get; set; }
        public int MenuItemsCount { get; set; }
        public int OrdersCount { get; set; }
        public int AddressesCount { get; set; }
        public int ActiveCartsCount { get; set; }
        public float AverageOrderValue { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
