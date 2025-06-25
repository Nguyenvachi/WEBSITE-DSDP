using E_Sport.Models;
using E_Sport.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace E_Sport.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Giả sử chỉ Admin được quản lý danh mục
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepo;
        private readonly IWebHostEnvironment _webHostEnvironment; // Thêm để xử lý file
        private readonly ApplicationDbContext _context; // Thêm để dễ dàng Include

        public CategoryController(ICategoryRepository categoryRepo, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            _categoryRepo = categoryRepo;
            _webHostEnvironment = webHostEnvironment; // Inject
            _context = context; // Inject
        }

        // GET: Hiển thị danh sách danh mục
        public async Task<IActionResult> Index()
        {
            // CẬP NHẬT: Include các thông tin cần thiết để View Index.cshtml mới hoạt động
            var categories = await _context.Categories
                                          .Include(c => c.ParentCategory)  // Để hiển thị tên cha
                                          .Include(c => c.SubCategories)   // Rất quan trọng cho logic đệ quy
                                          .Include(c => c.Products)        // Để đếm số sản phẩm
                                          .ToListAsync();
            return View(categories);
        }

        // GET: Hiển thị form thêm mới
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Add()
        {
            // CẬP NHẬT: Truyền danh sách danh mục cho dropdown "Danh mục cha"
            var categories = await _categoryRepo.GetAllAsync();
            ViewBag.ParentCategories = new SelectList(categories.OrderBy(c => c.Name), "Id", "Name");
            return View(new Category { IsActive = true, DisplayOrder = 0 });
        }

        // POST: Xử lý thêm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Add(Category category, IFormFile? imageUrlFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh đại diện nếu có
                if (imageUrlFile != null && imageUrlFile.Length > 0)
                {
                    category.ImageUrl = await SaveImage(imageUrlFile);
                }

                await _categoryRepo.AddAsync(category);
                TempData["success"] = "Thêm danh mục mới thành công!";
                return RedirectToAction("Index");
            }

            // Nếu không hợp lệ, tải lại danh sách cho dropdown
            var categories = await _categoryRepo.GetAllAsync();
            ViewBag.ParentCategories = new SelectList(categories.OrderBy(c => c.Name), "Id", "Name", category.ParentCategoryId);
            return View(category);
        }

        // GET: Hiển thị form chỉnh sửa
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // CẬP NHẬT: Truyền danh sách danh mục cho dropdown, loại trừ chính nó
            var allCategories = await _categoryRepo.GetAllAsync();
            ViewBag.ParentCategories = new SelectList(allCategories.Where(c => c.Id != id).OrderBy(c => c.Name), "Id", "Name", category.ParentCategoryId);

            return View(category);
        }

        // POST: Xử lý chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Category category, IFormFile? imageUrlFile)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh đại diện mới
                if (imageUrlFile != null && imageUrlFile.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(category.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, category.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    category.ImageUrl = await SaveImage(imageUrlFile);
                }

                await _categoryRepo.UpdateAsync(category);
                TempData["success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("Index");
            }

            // Nếu không hợp lệ, tải lại danh sách cho dropdown
            var allCategories = await _categoryRepo.GetAllAsync();
            ViewBag.ParentCategories = new SelectList(allCategories.Where(c => c.Id != id).OrderBy(c => c.Name), "Id", "Name", category.ParentCategoryId);
            return View(category);
        }

        // GET: Hiển thị trang xác nhận xóa
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Xử lý xóa
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category != null)
            {
                // Xóa ảnh của danh mục nếu có
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, category.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                await _categoryRepo.DeleteAsync(id);
                TempData["success"] = "Xóa danh mục thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // Phương thức helper để lưu ảnh
        private async Task<string> SaveImage(IFormFile image)
        {
            var imageFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");
            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(imageFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return "/images/categories/" + fileName; // Trả về đường dẫn tương đối
        }
    }
}

