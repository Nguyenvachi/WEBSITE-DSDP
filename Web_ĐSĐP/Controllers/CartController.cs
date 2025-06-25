using E_Sport.Models;
using E_Sport.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E_Sport.Controllers
{
    [Authorize] // Bảo vệ toàn bộ controller
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepo;
        private const string CART_KEY = "UserCart"; // Đặt tên key session rõ ràng

        public CartController(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        // GET: /Cart hoặc /Cart/Index
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            ViewBag.CartTotal = cart.Sum(item => item.Total);
            return View(cart);
        }

        // POST: /Cart/AddToCart - Xử lý khi submit từ form (ví dụ: trang chi tiết sản phẩm)
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm để bảo vệ form
        public async Task<IActionResult> AddToCart(int productId, int quantity) // SỬA: Bỏ tham số size
        {
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null) return NotFound("Sản phẩm không tồn tại.");

            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(p => p.ProductId == productId);

            // CẢI TIẾN: Kiểm tra tồn kho
            int quantityInCart = existingItem?.Quantity ?? 0;
            if (product.StockQuantity < quantityInCart + quantity)
            {
                TempData["ErrorMessage"] = $"Xin lỗi, chỉ còn {product.StockQuantity} sản phẩm '{product.Name}' trong kho. Bạn không thể thêm số lượng này.";
                return RedirectToAction("Display", "Product", new { id = productId });
            }

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity,
                    Unit = product.Unit // Thêm Unit
                });
            }

            SaveCartToSession(cart);
            TempData["SuccessMessage"] = $"Đã thêm '{product.Name}' vào giỏ hàng!";
            return RedirectToAction(nameof(Index)); // Chuyển về trang giỏ hàng
        }

        // POST: /Cart/AddToCartAjax - Xử lý khi thêm từ danh sách sản phẩm
        [HttpPost]
        public async Task<IActionResult> AddToCartAjax(int productId, int quantity = 1) // SỬA: Bỏ size
        {
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(p => p.ProductId == productId);

            // CẢI TIẾN: Kiểm tra tồn kho
            int quantityInCart = existingItem?.Quantity ?? 0;
            if (product.StockQuantity < quantityInCart + quantity)
            {
                return Json(new { success = false, message = $"Số lượng tồn kho không đủ. Chỉ còn {product.StockQuantity} sản phẩm." });
            }

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity,
                    Unit = product.Unit // Thêm Unit
                });
            }

            SaveCartToSession(cart);
            return Json(new
            {
                success = true,
                message = $"Đã thêm '{product.Name}' vào giỏ!",
                cartCount = cart.Sum(p => p.Quantity)
            });
        }

        // POST: /Cart/AjaxUpdateQuantity
        [HttpPost]
        public async Task<IActionResult> AjaxUpdateQuantity(int productId, int quantity) // SỬA: Bỏ size
        {
            if (quantity <= 0)
            {
                // Nếu số lượng <= 0, coi như xóa sản phẩm
                return await AjaxRemoveItem(productId);
            }

            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);

            if (item != null)
            {
                // CẢI TIẾN: Kiểm tra số lượng tồn kho
                var product = await _productRepo.GetByIdAsync(productId);
                if (product != null && quantity > product.StockQuantity)
                {
                    return Json(new { success = false, message = $"Chỉ còn {product.StockQuantity} sản phẩm tồn kho.", newQuantity = product.StockQuantity });
                }

                item.Quantity = quantity;
                SaveCartToSession(cart);
                return Json(new
                {
                    success = true,
                    itemId = item.ProductId,
                    newQuantity = item.Quantity,
                    itemTotal = (item.Price * item.Quantity).ToString("N0"),
                    cartItemCount = cart.Sum(p => p.Quantity),
                    cartTotal = cart.Sum(p => p.Total).ToString("N0")
                });
            }
            return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });
        }

        // POST: /Cart/AjaxRemoveItem
        [HttpPost]
        public async Task<IActionResult> AjaxRemoveItem(int productId) // SỬA: Bỏ size
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCartToSession(cart);
                return Json(new
                {
                    success = true,
                    message = "Đã xóa sản phẩm khỏi giỏ hàng.",
                    cartItemCount = cart.Sum(p => p.Quantity),
                    cartTotal = cart.Sum(p => p.Total).ToString("N0")
                });
            }
            return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });
        }

        // Các phương thức helper
        private List<CartItem> GetCartFromSession()
        {
            var sessionData = HttpContext.Session.GetString(CART_KEY);
            if (string.IsNullOrEmpty(sessionData))
                return new List<CartItem>();
            try
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(sessionData) ?? new List<CartItem>();
            }
            catch
            {
                HttpContext.Session.Remove(CART_KEY);
                return new List<CartItem>();
            }
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            if (cart == null) return;
            var sessionData = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString(CART_KEY, sessionData);
        }
    }
}
