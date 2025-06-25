using E_Sport.Models;
using E_Sport.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace E_Sport.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductRepository productRepo,
            ICategoryRepository categoryRepo,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context,
            ILogger<ProductController> logger)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Product
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                     .Include(p => p.Category)
                                     .Include(p => p.Region)
                                     .OrderByDescending(p => p.Id)
                                     .ToListAsync();
            return View(products);
        }

        // GET: /Admin/Product/Add
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Add()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewBag.Regions = new SelectList(await _context.Regions.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync(), "Id", "Name");
            return View(new Product { IsActive = true, StockQuantity = 0 });
        }

        // POST: /Admin/Product/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Add(Product product, IFormFile? imageUrlFile, List<IFormFile>? imageFiles)
        {
            // THÊM MỚI: Validation logic cho OldPrice
            if (product.OldPrice.HasValue && product.OldPrice <= product.Price)
            {
                ModelState.AddModelError("OldPrice", "Giá cũ phải lớn hơn giá bán hiện tại.");
            }

            if (ModelState.IsValid)
            {
                product.CreatedDate = DateTime.Now;
                if (imageUrlFile != null)
                {
                    product.ImageUrl = await SaveImage(imageUrlFile);
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                if (imageFiles != null && imageFiles.Any())
                {
                    foreach (var image in imageFiles)
                    {
                        if (image.Length > 0)
                        {
                            var imagePath = await SaveImage(image);
                            _context.ProductImages.Add(new ProductImage { ProductId = product.Id, Url = imagePath });
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                TempData["success"] = "Thêm sản phẩm mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu không hợp lệ, tải lại danh sách cho dropdowns
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", product.CategoryId);
            ViewBag.Regions = new SelectList(await _context.Regions.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", product.RegionId);
            return View(product);
        }

        // GET: /Admin/Product/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                                      .Include(p => p.Images)
                                      .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", product.CategoryId);
            ViewBag.Regions = new SelectList(await _context.Regions.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", product.RegionId);
            return View(product);
        }

        // POST: /Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product productViewModel, IFormFile? imageUrlFile, List<IFormFile>? imageFiles, List<int>? deletedImageIds)
        {
            if (id != productViewModel.Id) return NotFound();

            // THÊM MỚI: Validation logic cho OldPrice
            if (productViewModel.OldPrice.HasValue && productViewModel.OldPrice <= productViewModel.Price)
            {
                ModelState.AddModelError("OldPrice", "Giá cũ phải lớn hơn giá bán hiện tại.");
            }

            if (ModelState.IsValid)
            {
                var productFromDb = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
                if (productFromDb == null) return NotFound();

                // Cập nhật các thuộc tính
                productFromDb.Name = productViewModel.Name;
                productFromDb.Description = productViewModel.Description;
                productFromDb.Price = productViewModel.Price;
                productFromDb.OldPrice = productViewModel.OldPrice; // CẬP NHẬT: Thêm dòng này để lưu giá trị OldPrice
                productFromDb.CategoryId = productViewModel.CategoryId;
                productFromDb.RegionId = productViewModel.RegionId;
                productFromDb.ExpiryDate = productViewModel.ExpiryDate;
                productFromDb.Weight = productViewModel.Weight;
                productFromDb.Unit = productViewModel.Unit;
                productFromDb.Ingredients = productViewModel.Ingredients;
                productFromDb.StorageInstructions = productViewModel.StorageInstructions;
                productFromDb.IsFreshFood = productViewModel.IsFreshFood;
                productFromDb.StockQuantity = productViewModel.StockQuantity;
                productFromDb.IsActive = productViewModel.IsActive;
                productFromDb.IsFeatured = productViewModel.IsFeatured;
                productFromDb.UpdatedDate = DateTime.Now;

                // Xử lý ảnh đại diện mới
                if (imageUrlFile != null)
                {
                    if (!string.IsNullOrEmpty(productFromDb.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productFromDb.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                    }
                    productFromDb.ImageUrl = await SaveImage(imageUrlFile);
                }

                // Xử lý xóa ảnh chi tiết cũ
                if (deletedImageIds != null && deletedImageIds.Any())
                {
                    var imagesToDelete = productFromDb.Images.Where(i => deletedImageIds.Contains(i.Id)).ToList();
                    foreach (var image in imagesToDelete)
                    {
                        if (!string.IsNullOrEmpty(image.Url))
                        {
                            var oldDetailImagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.Url.TrimStart('/'));
                            if (System.IO.File.Exists(oldDetailImagePath)) System.IO.File.Delete(oldDetailImagePath);
                        }
                        _context.ProductImages.Remove(image);
                    }
                }

                // Xử lý thêm ảnh chi tiết mới
                if (imageFiles != null && imageFiles.Any())
                {
                    foreach (var image in imageFiles)
                    {
                        var imagePath = await SaveImage(image);
                        productFromDb.Images.Add(new ProductImage { Url = imagePath });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu không hợp lệ, tải lại danh sách cho dropdowns
            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", productViewModel.CategoryId);
            ViewBag.Regions = new SelectList(await _context.Regions.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync(), "Id", "Name", productViewModel.RegionId);
            return View(productViewModel);
        }

        // GET: Admin/Product/Display/5 (Hoặc Details)
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public async Task<IActionResult> Display(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                                     .Include(p => p.Category)
                                     .Include(p => p.Region)   // THÊM MỚI
                                     .Include(p => p.Images)
                                     .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }


        // GET: Admin/Product/Delete/5
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            // Include các thông tin cần thiết để hiển thị trang xác nhận xóa
            var product = await _context.Products
                                      .Include(p => p.Category)
                                      .Include(p => p.Region)
                                      .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                // Xóa ảnh đại diện (nếu có và không phải placeholder)
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != "/images/placeholder-image.png")
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                // Xóa các ảnh chi tiết (nếu có)
                if (product.Images != null && product.Images.Any())
                {
                    foreach (var img in product.Images)
                    {
                        if (!string.IsNullOrEmpty(img.Url))
                        {
                            var detailImagePath = Path.Combine(_webHostEnvironment.WebRootPath, img.Url.TrimStart('/'));
                            if (System.IO.File.Exists(detailImagePath))
                            {
                                System.IO.File.Delete(detailImagePath);
                            }
                        }
                    }
                    // EF Core sẽ tự động xóa các ProductImage liên quan khi xóa Product nếu có Cascade Delete
                    // Hoặc bạn cần xóa chúng khỏi _context.ProductImages trước
                    // _context.ProductImages.RemoveRange(product.Images);
                }

                _context.Products.Remove(product); // Hoặc await _productRepo.DeleteAsync(id);
                await _context.SaveChangesAsync();
                TempData["success"] = "Xóa sản phẩm thành công!";
            }
            else
            {
                TempData["error"] = "Không tìm thấy sản phẩm để xóa.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Product/DeleteProductImage/5 (Action cho AJAX xóa ảnh chi tiết)
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeleteProductImage(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
            {
                return Json(new { success = false, message = "Không tìm thấy ảnh." });
            }

            // Xóa file vật lý
            if (!string.IsNullOrEmpty(image.Url))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.Url.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi xóa file ảnh: {imagePath}");
                        return Json(new { success = false, message = "Lỗi khi xóa file ảnh trên server." });
                    }
                }
            }
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Ảnh đã được xóa." });
        }


        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        private async Task<string> SaveImage(IFormFile image)
        {
            var imageFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
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
            return "/images/products/" + fileName;
        }
    }
}
