using E_Sport.Models;
using E_Sport.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace E_Sport.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được quản lý Vùng miền
    public class RegionController : Controller
    {
        private readonly IRegionRepository _regionRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RegionController(IRegionRepository regionRepo, IWebHostEnvironment webHostEnvironment)
        {
            _regionRepo = regionRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Admin/Region
        public async Task<IActionResult> Index()
        {
            var regions = await _regionRepo.GetAllAsync();
            return View(regions);
        }

        // GET: /Admin/Region/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Region/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Region region, IFormFile? imageUrlFile)
        {
            if (ModelState.IsValid)
            {
                if (imageUrlFile != null)
                {
                    region.ImageUrl = await SaveImage(imageUrlFile);
                }
                await _regionRepo.AddAsync(region);
                TempData["success"] = "Thêm vùng miền mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(region);
        }

        // GET: /Admin/Region/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var region = await _regionRepo.GetByIdAsync(id);
            if (region == null)
            {
                return NotFound();
            }
            return View(region);
        }

        // POST: /Admin/Region/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Region region, IFormFile? imageUrlFile)
        {
            if (id != region.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingRegion = await _regionRepo.GetByIdAsync(id);
                if (existingRegion == null)
                {
                    return NotFound();
                }

                // Cập nhật các thuộc tính từ form
                existingRegion.Name = region.Name;
                existingRegion.Description = region.Description;
                existingRegion.IsActive = region.IsActive;

                // Xử lý upload ảnh mới
                if (imageUrlFile != null)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(existingRegion.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingRegion.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    existingRegion.ImageUrl = await SaveImage(imageUrlFile);
                }

                await _regionRepo.UpdateAsync(existingRegion);
                TempData["success"] = "Cập nhật vùng miền thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(region);
        }

        // GET: /Admin/Region/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var region = await _regionRepo.GetByIdAsync(id);
            if (region == null)
            {
                return NotFound();
            }
            return View(region);
        }

        // POST: /Admin/Region/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var region = await _regionRepo.GetByIdAsync(id);
            if (region != null)
            {
                // Xóa ảnh của vùng miền nếu có
                if (!string.IsNullOrEmpty(region.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, region.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                await _regionRepo.DeleteAsync(id);
                TempData["success"] = "Xóa vùng miền thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Phương thức helper để lưu ảnh
        private async Task<string> SaveImage(IFormFile image)
        {
            var imageFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "regions");
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

            return "/images/regions/" + fileName; // Trả về đường dẫn tương đối
        }
    }
}

