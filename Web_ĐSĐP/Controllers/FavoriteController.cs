using E_Sport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Sport.Controllers
{
    [Authorize(Roles = "Customer,Employee")]
    public class FavoriteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoriteController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.ProductId == productId && f.UserId == user.Id);

            if (existing != null)
            {
                _context.Favorites.Remove(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, favorited = false });
            }
            else
            {
                var fav = new Favorite
                {
                    ProductId = productId,
                    UserId = user.Id
                };
                _context.Favorites.Add(fav);
                await _context.SaveChangesAsync();
                return Json(new { success = true, favorited = true });
            }
        }

        // ✅ Action hiển thị danh sách sản phẩm yêu thích
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var favoriteProductIds = await _context.Favorites
                .Where(f => f.UserId == user.Id)
                .Select(f => f.ProductId)
                .ToListAsync();

            var products = await _context.Products
                .Where(p => favoriteProductIds.Contains(p.Id))
                .ToListAsync();

            return View(products); // ✅ Trả về List<Product>
        }
    }
}
