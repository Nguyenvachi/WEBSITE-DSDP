using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using E_Sport.Models;
using E_Sport.Models.ViewModels;
using E_Sport.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Sport.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;

        public HomeController(
            ILogger<HomeController> logger,
            IProductRepository productRepo,
            ICategoryRepository categoryRepo)
        {
            _logger = logger;
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            List<int>? selectedCategoryIds,
            List<string>? selectedOrigins,
            List<string>? selectedUnits,
            string? priceRange,
            string? sortBy)
        {
            // 1. Xử lý mức giá
            decimal? minPrice = null, maxPrice = null;
            if (!string.IsNullOrEmpty(priceRange))
            {
                var parts = priceRange.Split('-');
                if (parts.Length == 2)
                {
                    if (decimal.TryParse(parts[0], out var pmin)) minPrice = pmin;
                    if (decimal.TryParse(parts[1], out var pmax)) maxPrice = pmax;
                }
            }

            // 2. Lấy query gốc và áp filter
            var query = _productRepo.GetAllQueryable()
                .Include(p => p.Region)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search));
            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);
            if (selectedCategoryIds != null && selectedCategoryIds.Any())
                query = query.Where(p => selectedCategoryIds.Contains(p.CategoryId));
            if (selectedOrigins != null && selectedOrigins.Any())
                query = query.Where(p => selectedOrigins.Contains(p.Region!.Name));
            if (selectedUnits != null && selectedUnits.Any())
                query = query.Where(p => selectedUnits.Contains(p.Unit!));

            // 3. Sắp xếp
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedDate)
            };

            var products = await query.ToListAsync();

            // 4. Dữ liệu cho filters
            var categories = (await _categoryRepo.GetAllAsync()).ToList();
            var availableOrigins = await _productRepo.GetAllQueryable()
                .Where(p => p.Region != null)
                .Select(p => p.Region!.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
            var availableUnits = await _productRepo.GetAllQueryable()
                .Where(p => !string.IsNullOrEmpty(p.Unit))
                .Select(p => p.Unit!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // 5. ViewModel
            var vm = new ProductFilterViewModel
            {
                Products = products,
                Categories = categories,
                AvailableOrigins = availableOrigins,
                AvailableUnits = availableUnits,

                SelectedCategoryIds = selectedCategoryIds ?? new(),
                SelectedOrigins = selectedOrigins ?? new(),
                SelectedUnits = selectedUnits ?? new(),

                CurrentSearch = search,
                CurrentPriceRange = priceRange,
                CurrentSortBy = sortBy
            };

            // 6. Trả full view
            return View(vm);
        }

        public IActionResult Privacy()
            => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
