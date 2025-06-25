using Microsoft.AspNetCore.Mvc;
using E_Sport.Models;
using E_Sport.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace E_Sport.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")] // Cho phép cả Admin và Employee truy cập
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ApplicationDbContext _context; // Inject DbContext để dễ dàng Include

        public OrderController(IOrderRepository orderRepo, ApplicationDbContext context)
        {
            _orderRepo = orderRepo;
            _context = context;
        }

        // GET: Admin/Order
        // CẬP NHẬT: Thêm chức năng lọc theo trạng thái và tìm kiếm
        public async Task<IActionResult> Index(string? statusFilter, string? searchQuery)
        {
            // Bắt đầu với một IQueryable để xây dựng truy vấn động
            var ordersQuery = _context.Orders
                                      .Include(o => o.OrderDetails) // Lấy thông tin chi tiết để đếm số sản phẩm
                                      .AsQueryable();

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(statusFilter))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == statusFilter);
            }

            // Tìm kiếm theo tên, email, hoặc số điện thoại nếu có
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var lowerSearchQuery = searchQuery.ToLower();
                ordersQuery = ordersQuery.Where(o =>
                    (o.FullName != null && o.FullName.ToLower().Contains(lowerSearchQuery)) ||
                    (o.UserEmail != null && o.UserEmail.ToLower().Contains(lowerSearchQuery)) ||
                    (o.Phone != null && o.Phone.Contains(searchQuery)) // Phone không cần ToLower
                );
            }

            // Sắp xếp đơn hàng mới nhất lên đầu và thực thi truy vấn
            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchQuery = searchQuery;

            return View(orders);
        }

        // GET: Admin/Order/Details/5
        // CẬP NHẬT: Lấy đầy đủ thông tin chi tiết của đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                                      .Include(o => o.OrderDetails)
                                          .ThenInclude(od => od.Product) // Lấy thông tin Product từ OrderDetail
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: Admin/Order/EditStatus/5
        public async Task<IActionResult> EditStatus(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();

            // Cung cấp danh sách các trạng thái cho View
            ViewBag.PossibleStatuses = new SelectList(new[]
            {
                "PendingConfirmation", "PendingPayment", "Paid", "Processing", "Shipped", "Completed", "Cancelled"
            });

            return View(order);
        }

        // POST: Admin/Order/EditStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStatus(int id, string status)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();

            // CẢI TIẾN: Sử dụng một phương thức chuyên biệt để cập nhật trạng thái
            await _orderRepo.UpdateStatusAsync(id, status);
            TempData["success"] = $"Cập nhật trạng thái cho đơn hàng #{id} thành công.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Order/Delete/5
        [Authorize(Roles = "Admin")] // Chỉ Admin được xóa
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST: Admin/Order/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Chỉ Admin được xóa
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // CẢI TIẾN: Cân nhắc logic không nên xóa đơn hàng vĩnh viễn,
            // thay vào đó nên có trạng thái "Đã hủy" (Cancelled) hoặc "Đã xóa" (Deleted).
            // Tuy nhiên, nếu vẫn muốn xóa, logic hiện tại là đúng.
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            if (order != null)
            {
                // Logic hoàn lại số lượng sản phẩm (tùy chọn)
                // foreach (var detail in order.OrderDetails)
                // {
                //     var product = await _context.Products.FindAsync(detail.ProductId);
                //     if (product != null)
                //     {
                //         product.StockQuantity += detail.Quantity;
                //     }
                // }
                // await _context.SaveChangesAsync();

                await _orderRepo.DeleteAsync(id); // Phương thức DeleteAsync trong repo sẽ xóa order và orderdetails (do cascade delete)
                TempData["success"] = $"Đã xóa đơn hàng #{id} thành công.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
