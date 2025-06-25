using E_Sport.Models;
using E_Sport.Services;
using Microsoft.AspNetCore.Mvc;

namespace E_Sport.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;

        public PaymentController(IVnPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        // PHƯƠNG THỨC CŨ GÂY LỖI - ĐÃ ĐƯỢC VÔ HIỆU HÓA
        // Toàn bộ logic giờ đã nằm trong OrderController
        /*
        [HttpPost]
        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            // Lỗi xảy ra ở dòng dưới vì phương thức này không còn tồn tại
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
            return Redirect(url);
        }
        */

        // PHƯƠNG THỨC CŨ GÂY LỖI - ĐÃ ĐƯỢC VÔ HIỆU HÓA
        // Callback đã được xử lý trong OrderController
        /*
        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.Success)
            {
                ViewBag.Message = "Thanh toán thành công! Mã giao dịch: " + response.TransactionId;
            }
            else
            {
                ViewBag.Message = "Thanh toán thất bại. Vui lòng thử lại.";
            }

            return View("PaymentResult", response); 
        }
        */
    }
}
