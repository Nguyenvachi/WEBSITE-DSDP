using E_Sport.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Sport.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var allUsers = await _userManager.Users.ToListAsync();
            var customers = new List<ApplicationUser>();
            var rolesDict = new Dictionary<string, string>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Chỉ lấy người dùng có vai trò là Customer
                if (roles.Contains("Customer"))
                {
                    customers.Add(user);
                }

                // Lưu vai trò hiện tại vào dictionary
                rolesDict[user.Id] = roles.FirstOrDefault() ?? "Không xác định";
            }

            ViewBag.RolesPerUser = rolesDict;
            return View(customers);
        }

        [HttpPost]
        public async Task<IActionResult> PromoteToEmployee(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");
            if (isCustomer)
            {
                await _userManager.RemoveFromRoleAsync(user, "Customer");
                await _userManager.AddToRoleAsync(user, "Employee");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> UpdateRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault();

            // Chỉ cho phép nâng từ Customer lên Employee
            if (currentRole == "Customer" && role == "Employee")
            {
                await _userManager.RemoveFromRoleAsync(user, currentRole);
                await _userManager.AddToRoleAsync(user, "Employee");
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ApplicationUser());
        }

        [HttpPost]
        public async Task<IActionResult> Create(ApplicationUser model)
        {
            if (ModelState.IsValid)
            {
                model.UserName = model.Email;
                model.EmailConfirmed = false;

                var result = await _userManager.CreateAsync(model, "Thanh@123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(model, "Customer");
                    TempData["success"] = "Thêm người dùng thành công!";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ApplicationUser model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            await _userManager.UpdateAsync(user);
            TempData["success"] = "Cập nhật người dùng thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

    }
}
