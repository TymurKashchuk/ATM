using AtmSimulator.Services;
using AtmSimulator.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AtmSimulator.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _adminService;
        private readonly IConfiguration _configuration;

        public AdminController(AdminService adminService, IConfiguration configuration)
        {
            _adminService = adminService;
            _configuration = configuration;
        }

        private IActionResult? CheckAdmin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
                return RedirectToAction("Login");
            return null;
        }

        public async Task<IActionResult> Index(string? search)
        {
            if (CheckAdmin() is { } redirect) return redirect;

            var accounts = await _adminService.GetAccountsAsync(search);

            var viewModel = new AdminAccountListViewModel
            {
                Accounts = accounts,
                SearchQuery = search
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleBlock(int accountId)
        {
            if (CheckAdmin() is { } redirect) return redirect;

            await _adminService.ToggleBlockAsync(accountId);
            TempData["Success"] = "Статус картки змінено";
            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {
            if (CheckAdmin() is { } redirect) return redirect;
            return View(new CreateAccountViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAccountViewModel model)
        {
            if (CheckAdmin() is { } redirect) return redirect;

            if (!ModelState.IsValid) return View(model);

            try
            {
                await _adminService.CreateAccountAsync(model);
                TempData["Success"] = "Акаунт успішно створено";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string password)
        {
            if (password == _configuration["Admin:Password"])
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction("Index");
            }

            ViewBag.Error = "Невірний пароль";
            return View();
        }

        public IActionResult AdminLogout()
        {
            HttpContext.Session.Remove("IsAdmin");
            return RedirectToAction("Login");
        }
    }
}