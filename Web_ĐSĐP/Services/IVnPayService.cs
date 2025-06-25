using E_Sport.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Sport.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, IUrlHelper urlHelper, Order order);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }

}
