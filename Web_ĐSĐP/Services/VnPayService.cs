using E_Sport.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace E_Sport.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, IUrlHelper urlHelper, Order order)
        {
            var pay = new VnPayLibrary();
            pay.AddRequestData("vnp_Version", _config["Vnpay:Version"]);
            pay.AddRequestData("vnp_Command", _config["Vnpay:Command"]);
            pay.AddRequestData("vnp_TmnCode", _config["Vnpay:TmnCode"]);
            pay.AddRequestData("vnp_Amount", ((long)order.TotalAmount * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", order.OrderDate.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _config["Vnpay:CurrCode"]);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _config["Vnpay:Locale"]);
            pay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang: {order.Id}");
            pay.AddRequestData("vnp_OrderType", "other");

            // ReturnUrl chuẩn
            var returnUrl = urlHelper.Action("PaymentCallbackVnpay", "Order", null, context.Request.Scheme);
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);
            pay.AddRequestData("vnp_TxnRef", order.Id.ToString());

            var paymentUrl = pay.CreateRequestUrl(_config["Vnpay:BaseUrl"], _config["Vnpay:HashSecret"]);
            return paymentUrl;
        }


        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();

            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    pay.AddResponseData(key, value);
                }
            }

            var orderId = pay.GetResponseData("vnp_TxnRef");
            var transId = pay.GetResponseData("vnp_TransactionNo");
            var responseCode = pay.GetResponseData("vnp_ResponseCode");
            var secureHash = collections.FirstOrDefault(x => x.Key == "vnp_SecureHash").Value;
            var orderInfo = pay.GetResponseData("vnp_OrderInfo");
            var isValid = pay.ValidateSignature(secureHash!, _config["Vnpay:HashSecret"]);

            return new PaymentResponseModel
            {
                Success = isValid,
                PaymentMethod = "VNPAY",
                OrderDescription = orderInfo,
                OrderId = orderId,
                PaymentId = transId,
                TransactionId = transId,
                Token = secureHash,
                VnPayResponseCode = responseCode
            };
        }
    }
}
