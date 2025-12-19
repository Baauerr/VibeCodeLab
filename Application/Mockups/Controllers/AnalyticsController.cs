using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mockups.Models.Analytics;
using Mockups.Repositories.Addresses;
using Mockups.Repositories.Carts;
using Mockups.Repositories.MenuItems;
using Mockups.Repositories.Orders;
using Mockups.Storage;

namespace Mockups.Controllers
{
    [Route("analytics")]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartsRepository _cartsRepository;

        public AnalyticsController(ApplicationDbContext context, CartsRepository cartsRepository)
        {
            _context = context;
            _cartsRepository = cartsRepository;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var usersCount = await _context.Users.CountAsync();
            var menuItemsCount = await _context.MenuItems.CountAsync();
            var ordersCount = await _context.Orders.CountAsync();
            var addressesCount = await _context.Addresses.CountAsync();

            // Average order value (ignores discount for simplicity)
            var avg = 0f;
            if (ordersCount > 0)
            {
                avg = (float)(await _context.Orders.AverageAsync(o => (double)o.Cost));
            }

            // CartsRepository stores carts in-memory; attempt to infer active carts count
            int activeCarts = 0;
            try
            {
                // Reflectively access private field _carts if available as a last resort
                var field = typeof(CartsRepository).GetField(
                    "_carts",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );
                if (field != null)
                {
                    var value = field.GetValue(_cartsRepository) as System.Collections.ICollection;
                    if (value != null)
                        activeCarts = value.Count;
                }
            }
            catch
            {
                activeCarts = 0;
            }

            var model = new AnalyticsSummaryViewModel
            {
                UsersCount = usersCount,
                MenuItemsCount = menuItemsCount,
                OrdersCount = ordersCount,
                AddressesCount = addressesCount,
                ActiveCartsCount = activeCarts,
                AverageOrderValue = avg,
            };

            return Ok(model);
        }

        [HttpGet("details")]
        public async Task<IActionResult> Details()
        {
            // return simple last-7-days stats: dates, orders count, revenue (cost after discount)
            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-6);

            var orders = await _context.Orders.Where(o => o.CreationTime >= from).ToListAsync();

            var dates = new List<string>();
            var ordersCount = new List<int>();
            var revenue = new List<float>();

            for (int i = 0; i < 7; i++)
            {
                var d = from.AddDays(i);
                dates.Add(d.ToString("yyyy-MM-dd"));
                var dayOrders = orders.Where(o => o.CreationTime.Date == d).ToList();
                ordersCount.Add(dayOrders.Count);
                revenue.Add(dayOrders.Sum(o => o.Cost * (100 - o.Discount) / 100));
            }

            return Ok(
                new
                {
                    dates,
                    ordersCount,
                    revenue,
                }
            );
        }

        [HttpGet("insights")]
        public async Task<IActionResult> Insights()
        {
            // last 7 days
            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-6);

            var orders = await _context.Orders
                .Where(o => o.CreationTime >= from)
                .Include(o => o.Id)
                .ToListAsync();

            // hourly distribution (0..23)
            var hours = new int[24];
            foreach (var o in orders)
            {
                var h = o.CreationTime.ToUniversalTime().Hour;
                hours[h]++;
            }

            // top-5 items across order menu items
            var joins = await _context.OrderMenuItems
                .Where(omi => omi.Order != null && omi.Order.CreationTime >= from)
                .Include(omi => omi.Item)
                .ToListAsync();

            var top = joins
                .GroupBy(x => new { x.Item.Id, x.Item.Name })
                .Select(g => new { name = g.Key.Name, quantity = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.quantity)
                .Take(5)
                .ToList();

            return Ok(new { hours, top });
        }

        [HttpGet("view")]
        public async Task<IActionResult> Index()
        {
            var resp = await Summary();
            if (resp is OkObjectResult ok && ok.Value is AnalyticsSummaryViewModel model)
            {
                return View(model);
            }

            // fallback: build minimal model
            var model2 = new AnalyticsSummaryViewModel { GeneratedAt = DateTime.UtcNow };
            return View(model2);
        }
    }
}
