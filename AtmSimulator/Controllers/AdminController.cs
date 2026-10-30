using AtmSimulator.Filters;
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

        [AdminAuthorize]
        public async Task<IActionResult> Index(string? search)
        {
            var accounts = await _adminService.GetAccountsAsync(search);

            var viewModel = new AdminAccountListViewModel
            {
                Accounts = accounts,
                SearchQuery = search
            };

            return View(viewModel);
        }

        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> ToggleBlock(int accountId)
        {
            await _adminService.ToggleBlockAsync(accountId);
            TempData["Success"] = "Статус картки змінено";
            return RedirectToAction("Index");
        }

        [AdminAuthorize]
        public IActionResult Create()
        {
            return View(new CreateAccountViewModel());
        }

        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Create(CreateAccountViewModel model)
        {
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
        [AdminAuthorize]
        public async Task<IActionResult> CashStorage()
        {
            var viewModel = await _adminService.GetCashStorageDetailsAsync();
            return View(viewModel);
        }

        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> RefillCash(int denomination, int count)
        {
            try
            {
                await _adminService.RefillCassetteAsync(denomination, count);
                TempData["Success"] = $"Касету {denomination} ₴ успішно поповнено на {count} купюр";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(CashStorage));
        }

        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> WithdrawCash(int denomination, int count)
        {
            try
            {
                await _adminService.WithdrawCassetteAsync(denomination, count);
                TempData["Success"] = $"Інкасовано {count} купюр номіналом {denomination} ₴ (на суму {denomination * count:N2} ₴)";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(CashStorage));
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
