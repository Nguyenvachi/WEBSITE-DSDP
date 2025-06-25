using E_Sport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Sport.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        // 👉 Xem chi tiết đơn hàng
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id); // ✅ check đúng user

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // 👉 Hủy đơn nếu chưa duyệt
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null || order.Status == "Approved")
            {
                return BadRequest("Không thể hủy đơn đã được duyệt!");
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return RedirectToAction("OrderHistory");
        }

        // ✅ Action Lịch sử đơn hàng
        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);

            var orders = _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders); // sẽ đẩy sang View OrderHistory.cshtml
        }
    }
}
