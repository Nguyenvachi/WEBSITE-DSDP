using Microsoft.AspNetCore.Mvc;

namespace E_Sport.Controllers
{
    public class AccountController : Controller
    {
        [Route("Account/AccessDenied")]
        public IActionResult AccessDenied(string returnUrl)
        {
            // Redirect vĩnh viễn đến trang Razor Page gốc
            return Redirect($"/Identity/Account/AccessDenied?ReturnUrl={returnUrl}");
        }
    }
}
