using E_Sport.Models;
using E_Sport.Models.ViewModels;
using E_Sport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace E_Sport.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IVnPayService _vnPayService;
        private readonly ILogger<OrderController> _logger;
        private const string CART_KEY = "UserCart";

        public OrderController(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager,
                               IEmailSender emailSender,
                               IVnPayService vnPayService,
                               ILogger<OrderController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _vnPayService = vnPayService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }
            var user = await _userManager.GetUserAsync(User);
            var model = new OrderCheckoutModel
            {
                CartItems = cart,
                TotalAmount = cart.Sum(item => item.Total),
                FullName = user?.FullName,
                Phone = user?.PhoneNumber,
                ShippingAddress = user?.Address
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrderAjax(OrderCheckoutModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, error = $"Dữ liệu không hợp lệ: {errors}" });
            }

            var cart = GetCartFromSession();
            if (!cart.Any())
                return Json(new { success = false, error = "Giỏ hàng của bạn đã trống." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, error = "Không tìm thấy thông tin người dùng." });

            string fullShippingAddress = $"{model.ShippingAddress}, {model.WardName}, {model.DistrictName}, {model.ProvinceName}";
            var order = new Order
            {
                UserId = user.Id,
                UserEmail = user.Email,
                FullName = model.FullName,
                ShippingAddress = fullShippingAddress,
                Phone = model.Phone,
                Note = model.Note,
                PaymentMethod = model.PaymentMethod,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(item => item.Total),
                Status = (model.PaymentMethod == "VNPAY") ? "PendingPayment" : "PendingConfirmation",
                OrderDetails = cart.Select(p => new OrderDetail
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    Unit = p.Unit
                }).ToList()
            };

            _context.Orders.Add(order);

            // Chỉ trừ kho nếu KHÔNG phải VNPAY
            if (model.PaymentMethod != "VNPAY")
            {
                foreach (var item in order.OrderDetails)
                {
                    var productToUpdate = await _context.Products.FindAsync(item.ProductId);
                    if (productToUpdate == null || productToUpdate.StockQuantity < item.Quantity)
                        return Json(new { success = false, error = $"Sản phẩm '{item.ProductName}' không đủ số lượng." });

                    productToUpdate.StockQuantity -= item.Quantity;
                }
            }

            try
            {
                await _context.SaveChangesAsync();

                // Nếu là VNPAY thì redirect luôn, KHÔNG trừ kho lúc này!
                if (model.PaymentMethod == "VNPAY")
                {
                    var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, Url, order);
                    if (!string.IsNullOrEmpty(paymentUrl))
                        return Json(new { success = true, redirectUrl = paymentUrl });

                    _logger.LogError("Không thể tạo URL VNPAY cho đơn hàng {orderId}", order.Id);
                    return Json(new { success = false, error = "Không thể khởi tạo thanh toán VNPAY." });
                }
                else
                {
                    await SendConfirmationEmail(order);
                    HttpContext.Session.Remove(CART_KEY);
                    return Json(new { success = true, redirectUrl = Url.Action("Success", "Order", new { id = order.Id }) });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu đơn hàng {orderId}", order.Id);
                return Json(new { success = false, error = "Lỗi hệ thống khi tạo đơn hàng." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null || !response.Success || string.IsNullOrEmpty(response.OrderId))
            {
                TempData["ErrorMessage"] = response?.OrderDescription ?? "Giao dịch VNPAY không thành công.";
                return View("PaymentFail");
            }

            if (!int.TryParse(response.OrderId, out int orderId))
            {
                TempData["ErrorMessage"] = "Mã đơn hàng VNPAY không hợp lệ.";
                return View("PaymentFail");
            }

            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return View("PaymentFail");
            }

            // Đơn đã thanh toán rồi thì không xử lý lại
            if (order.Status == "Paid")
            {
                TempData["SuccessMessage"] = $"Đơn hàng #{order.Id} đã được xử lý thanh toán trước đó.";
                return View("Success", order);
            }

            // TRỪ KHO Ở ĐÂY (sau khi thanh toán thành công)
            foreach (var item in order.OrderDetails)
            {
                var productToUpdate = await _context.Products.FindAsync(item.ProductId);
                if (productToUpdate == null || productToUpdate.StockQuantity < item.Quantity)
                {
                    order.Status = "Cancelled";
                    order.Note += $"\nHủy tự động do hết hàng ({item.ProductName}) lúc thanh toán.";
                    await _context.SaveChangesAsync();
                    TempData["ErrorMessage"] = $"Rất tiếc, sản phẩm '{item.ProductName}' đã hết hàng. Đơn hàng đã được hủy.";
                    return View("PaymentFail");
                }
                productToUpdate.StockQuantity -= item.Quantity;
            }

            order.Status = "Paid";
            await _context.SaveChangesAsync();

            await SendConfirmationEmail(order, isPaid: true);
            HttpContext.Session.Remove(CART_KEY);

            TempData["SuccessMessage"] = $"Thanh toán thành công cho đơn hàng #{order.Id}!";
            return View("Success", order);
        }

        public async Task<IActionResult> Success(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            return order == null ? NotFound() : View(order);
        }

        public IActionResult PaymentFail()
        {
            return View();
        }

        private List<CartItem> GetCartFromSession()
        {
            var sessionData = HttpContext.Session.GetString(CART_KEY);
            if (string.IsNullOrEmpty(sessionData)) return new List<CartItem>();
            try
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(sessionData) ?? new List<CartItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi deserialize giỏ hàng.");
                HttpContext.Session.Remove(CART_KEY);
                return new List<CartItem>();
            }
        }

        private async Task SendConfirmationEmail(Order order, bool isPaid = false)
        {
            var user = await _userManager.FindByIdAsync(order.UserId);
            if (user == null || string.IsNullOrEmpty(user.Email)) return;

            try
            {
                string subject = isPaid ? $"Thanh toán thành công cho đơn hàng #{order.Id}" : $"Xác nhận đơn hàng #{order.Id}";
                string emailBody = ConstructOrderConfirmationEmailBody(order, Url, isPaid);
                await _emailSender.SendEmailAsync(user.Email, $"{subject} - Đặc Sản Địa Phương", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email xác nhận cho đơn hàng {order.Id}.");
            }
        }

        private string ConstructOrderConfirmationEmailBody(Order order, IUrlHelper url, bool isPaid = false)
        {
            var domain = $"{Request.Scheme}://{Request.Host}";
            string shopName = "Đặc Sản Địa Phương";
            string shopLogoUrl = $"{domain}/images/logo.png";

            var statusVN = order.Status switch { "PendingConfirmation" => "Chờ xác nhận", "PendingPayment" => "Chờ thanh toán", "Paid" => "Đã thanh toán", "Processing" => "Đang xử lý", "Shipped" => "Đang giao hàng", "Completed" => "Đã hoàn thành", "Cancelled" => "Đã hủy", _ => order.Status };
            var paymentMethodVN = order.PaymentMethod switch { "COD" => "Thanh toán khi nhận hàng (COD)", "VNPAY" => "Thanh toán qua VNPAY", "BankTransfer" => "Chuyển khoản ngân hàng", _ => order.PaymentMethod };

            var productDetailsHtml = string.Join("", order.OrderDetails.Select(item => $@"<tr style='border-bottom: 1px solid #eee;'><td style='padding: 10px 0;'><div style='display: flex; align-items: center;'><img src='{domain}{url.Content(item.ImageUrl ?? "/images/placeholder-image.png")}' alt='{item.ProductName}' width='65' height='65' style='margin-right: 15px; border: 1px solid #ddd; border-radius: 8px; object-fit: cover;'/><div style='line-height: 1.5;'><strong style='font-size: 1em; color: #333;'>{item.ProductName}</strong><br/><span style='color: #666; font-size: 0.9em;'>{item.Quantity} {(string.IsNullOrEmpty(item.Unit) ? "sản phẩm" : item.Unit.ToLower())} x {item.Price:N0} ₫</span></div></div></td><td style='padding: 10px 0; text-align: right; font-weight: bold; color: #333;'>{(item.Price * item.Quantity):N0} ₫</td></tr>"));

            string headerMessage = isPaid ? "Thanh toán thành công" : "Đặt hàng thành công";

            return $@"<!DOCTYPE html><html><head><meta charset='UTF-8'><style>body{{font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f7f7f7; padding: 20px;}}.container{{max-width: 600px; margin: auto; background-color: #ffffff; border: 1px solid #ddd; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.05);}}.header{{background-color: #2b8a3e; color: white; padding: 20px; text-align: center; border-top-left-radius: 8px; border-top-right-radius: 8px;}}.header h2{{margin:0; font-size: 24px;}}.content{{padding: 30px;}}.content h3{{color: #2b8a3e; border-bottom: 2px solid #f0f0f0; padding-bottom: 10px; margin-top: 30px;}}.order-info p, .shipping-info p{{margin: 5px 0;}}.order-info strong, .shipping-info strong{{color: #555; min-width:120px; display: inline-block;}}.products-table{{width: 100%; border-collapse: collapse; margin-top: 15px;}}.footer{{background-color: #f2f2f2; padding: 20px; text-align: center; font-size: 0.8em; color: #777; border-bottom-left-radius: 8px; border-bottom-right-radius: 8px;}}.button{{background-color: #f59f00; color: white !important; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; margin-top: 20px;}}</style></head><body><div class='container'><div class='header'><img src='{shopLogoUrl}' alt='{shopName} Logo' style='max-height: 60px; margin-bottom: 10px;'/><h2 >🎉 {headerMessage}!</h2></div><div class='content'><p>Cảm ơn bạn, <strong>{order.FullName}</strong>, đã đặt hàng tại {shopName}.</p><h3 id='order-summary'>🧾 Thông tin đơn hàng</h3><div class='order-info'><p><strong>Mã đơn hàng:</strong> <span style='color: #E91E63; font-weight: bold;'>#{order.Id}</span></p><p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p><p><strong>Trạng thái:</strong> <span style='font-weight:bold;'>{statusVN}</span></p><p><strong>Thanh toán:</strong> {paymentMethodVN}</p></div><h3 id='shipping-info'>📦 Thông tin giao hàng</h3><div class='shipping-info'><p><strong>Người nhận:</strong> {order.FullName}</p><p><strong>Số điện thoại:</strong> {order.Phone}</p><p><strong>Địa chỉ:</strong> {order.ShippingAddress}</p>{(string.IsNullOrEmpty(order.Note) ? "" : $"<p><strong>Ghi chú:</strong> {order.Note}</p>")}</div><h3 id='product-details'>🛍️ Chi tiết sản phẩm</h3><table class='products-table'>{productDetailsHtml}</table><div style='text-align: right; margin-top: 20px; padding-top:15px; border-top:2px solid #eee;'><strong style='font-size: 1.2em;'>Tổng cộng: <span style='color: #E91E63; font-size: 1.4em;'>{order.TotalAmount:N0} ₫</span></strong></div><hr style='margin:30px 0; border:0; border-top: 1px solid #eee;'/><p style='color:#555; font-size:0.95em;'>Chúng tôi sẽ xử lý đơn hàng của bạn sớm nhất có thể. Bạn có thể theo dõi trạng thái đơn hàng trong lịch sử mua hàng của mình.</p><p style='text-align:center;'><a href='{domain}{Url.Action("OrderHistory", "Customer")}' class='button'>Xem lịch sử đơn hàng</a></p></div><div class='footer'>&copy; {DateTime.Now.Year} {shopName}. Trân trọng cảm ơn!</div></div></body></html>";
        }
    }
}
