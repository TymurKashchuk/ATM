using AtmSimulator.Services;
using AtmSimulator.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AtmSimulator.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

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
        public async Task<IActionResult> ToggleBlock(int accountId)
        {
            await _adminService.ToggleBlockAsync(accountId);
            TempData["Success"] = "Статус картки змінено";
            return RedirectToAction("Index");
        }

        public IActionResult Create() => View(new CreateAccountViewModel());

        [HttpPost]
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
    }
}