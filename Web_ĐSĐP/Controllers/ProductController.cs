using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using E_Sport.Models;
using E_Sport.Models.ViewModels;
using E_Sport.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Sport.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;
        private readonly IReviewRepository _reviewRepo;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ApplicationDbContext context,
            IReviewRepository reviewRepo)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
            _reviewRepo = reviewRepo;
        }

        // GET: /Product/Display/5
        public async Task<IActionResult> Display(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Region)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                return NotFound();

            // Increment view count
            product.ViewCount++;
            await _context.SaveChangesAsync();

            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.IsActive)
                .Include(p => p.Region)
                .OrderByDescending(p => p.ViewCount)
                .Take(4)
                .ToListAsync();

            var packs = (product.PackOptions ?? new List<string>()).ToList();
            var selectedPack = packs.FirstOrDefault() ?? string.Empty;

            var vm = new ProductDisplayViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts,
                PromoCode = product.PromoCode ?? string.Empty,
                ShippingFee = product.ShippingFee,
                ShippingRegion = product.Region?.Name ?? string.Empty,
                PackOptions = packs,
                SelectedPack = selectedPack,
                TotalReviews = product.Reviews?.Count ?? 0,
                AverageRating = product.Reviews?.Any() == true ? product.Reviews.Average(r => r.Rating) : 0.0,
                TotalSold = product.TotalSold,
                NewReview = new Review { ProductId = id } // Khởi tạo ProductId cho form đánh giá
            };

            // Load reviews for display
            ViewBag.Reviews = _reviewRepo.GetByProduct(id);
            return View(vm);
        }

        // POST: /Product/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddReview([Bind(Prefix = "NewReview")] Review model)
        {
            if (ModelState.IsValid)
            {
                _reviewRepo.Add(model);
                _reviewRepo.SaveChanges();
            }
            // PRG: Redirect back to the Display action to refresh the page
            return RedirectToAction(nameof(Display), new { id = model.ProductId });
        }
    }
}
