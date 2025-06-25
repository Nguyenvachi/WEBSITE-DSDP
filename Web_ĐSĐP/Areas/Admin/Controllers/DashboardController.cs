using E_Sport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace E_Sport.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager; 
        }

        public async Task<IActionResult> Index()
        {
            // Tổng số đơn hàng
            var totalOrders = await _context.Orders.CountAsync();

            // ✅ Doanh thu tổng: chỉ tính đơn đã duyệt hoặc đã thanh toán
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Approved" || o.Status == "Paid")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;


            // ✅ Doanh thu tháng hiện tại
            var now = DateTime.Now;
            var currentMonthRevenue = await _context.Orders
                .Where(o =>
                    (o.Status == "Approved" || o.Status == "Paid") &&
                    o.OrderDate.Month == now.Month &&
                    o.OrderDate.Year == now.Year)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;


            // ✅ Doanh thu theo phương thức thanh toán
            var codRevenue = await _context.Orders
                .Where(o => o.PaymentMethod == "COD" && (o.Status == "Approved" || o.Status == "Paid"))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var bankRevenue = await _context.Orders
                .Where(o => o.PaymentMethod == "BankTransfer" && (o.Status == "Approved" || o.Status == "Paid"))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var vnpayRevenue = await _context.Orders
                .Where(o => o.PaymentMethod == "VNPAY" && (o.Status == "Approved" || o.Status == "Paid"))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;


            // ✅ Đếm người dùng có vai trò Customer
            var users = await _userManager.Users.ToListAsync();
            int customerCount = 0;
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Customer"))
                {
                    customerCount++;
                }
            }

            // Top 5 sản phẩm bán chạy
            var top5 = _context.OrderDetails
                .GroupBy(x => x.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    ImageUrl = g.First().Product.ImageUrl,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();

            ViewBag.TopProducts = JsonConvert.SerializeObject(top5);
            ViewBag.TopProductList = top5;

            // Đơn hàng theo tháng
            var ordersByMonth = await _context.Orders
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            // Đơn hàng theo trạng thái
            var statusDict = new Dictionary<string, string>
            {
                { "Pending", "Chờ xác nhận" },
                { "Approved", "Đã duyệt" },
                { "Cancelled", "Đã hủy" },
                { "Paid", "Đã thanh toán" }
            };

            var ordersByStatus = _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new {
                    Status = statusDict.ContainsKey(g.Key) ? statusDict[g.Key] : g.Key,
                    Count = g.Count()
                }).ToList();

            // ✅ Gửi dữ liệu sang View
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.CurrentMonthRevenue = currentMonthRevenue;
            ViewBag.CodRevenue = codRevenue;
            ViewBag.BankRevenue = bankRevenue;
            ViewBag.VnPayRevenue = vnpayRevenue;
            ViewBag.TotalUsers = customerCount;
            ViewBag.OrdersByMonth = JsonConvert.SerializeObject(ordersByMonth);
            ViewBag.OrdersByStatus = JsonConvert.SerializeObject(ordersByStatus);

            return View();
        }
    }
}
