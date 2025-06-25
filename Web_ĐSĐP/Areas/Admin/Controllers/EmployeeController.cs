using E_Sport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace E_Sport.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;

        public EmployeeController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            var allUsers = _userManager.Users.ToList();
            var employees = new List<ApplicationUser>();
            var rolesDict = new Dictionary<string, string>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any() && roles.Contains("Employee"))
                {
                    employees.Add(user);
                }

                if (roles.Any())
                {
                    rolesDict[user.Id] = roles.First(); // lấy role đầu tiên
                }
                else
                {
                    rolesDict[user.Id] = "";
                }
            }

            ViewBag.RolesPerUser = rolesDict;
            return View(employees);
        }
        public IActionResult Add()
        {
            return View(new ApplicationUser());
        }

        [HttpPost]
        public async Task<IActionResult> Add(ApplicationUser model)
        {
            if (ModelState.IsValid)
            {
                model.EmailConfirmed = false;
                model.UserName = model.Email;

                // Tạo với mật khẩu tạm để bypass, sẽ reset sau
                var result = await _userManager.CreateAsync(model, "Thanh@123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(model, "Employee");

                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(model);
                    var confirmLink = Url.Action("ConfirmAccount", "Employee", new
                    {
                        area = "Admin",
                        userId = model.Id,
                        token = token
                    }, protocol: HttpContext.Request.Scheme);

                    var emailBody = $@"
                        <p>Xin chào {model.FullName},</p>
                        <p>Vui lòng xác nhận tài khoản và đặt mật khẩu bằng cách nhấn vào liên kết bên dưới:</p>
                        <p><a href='{confirmLink}'>Xác nhận tài khoản</a></p>";

                    //await _emailSender.SendEmailAsync(model.Email, "Xác nhận tài khoản nhân viên", emailBody);
                    try
                    {
                        await _emailSender.SendEmailAsync(model.Email, "Xác nhận tài khoản nhân viên", emailBody);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Gửi email thất bại: " + ex.Message);
                        return View(model);
                    }

                    TempData["success"] = "Tạo tài khoản thành công!";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new EditEmployeeViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? ""
            };

            ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name));

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(EditEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(_roleManager.Roles.Select(r => r.Name));
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            // Xóa role cũ
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Gán role mới
            await _userManager.AddToRoleAsync(user, model.Role);

            await _userManager.UpdateAsync(user);

            return RedirectToAction("Index");
        }



        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
                await _userManager.DeleteAsync(user);

            return RedirectToAction("Index");
        }


        // === Xác nhận email + đặt mật khẩu ===

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ConfirmAccount(string userId, string token)
        {
            var model = new ConfirmAccountViewModel
            {
                UserId = userId,
                Token = token
            };
            return View("SetPassword", model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmAccount(ConfirmAccountViewModel model)
        {
            if (!ModelState.IsValid) return View("SetPassword", model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var confirmResult = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (!confirmResult.Succeeded)
            {
                ModelState.AddModelError("", "Xác nhận thất bại");
                return View("SetPassword", model);
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);

            if (resetResult.Succeeded)
                return RedirectToAction("Login", "Account", new { area = "Identity" });

            foreach (var error in resetResult.Errors)
                ModelState.AddModelError("", error.Description);

            return View("SetPassword", model);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);

            var isAdmin = currentUserRoles.Contains("Admin");
            var isEmployee = currentUserRoles.Contains("Employee");

            // Nếu người dùng hiện tại là Employee thì KHÔNG được nâng người khác lên Admin
            if (isEmployee && role == "Admin")
            {
                TempData["error"] = "Bạn không có quyền cấp vai trò Admin.";
                return RedirectToAction("Index");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            TempData["success"] = "Cập nhật vai trò thành công.";
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

    }
}
