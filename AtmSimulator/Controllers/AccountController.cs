using AtmSimulator.Data;
using AtmSimulator.Services;
using AtmSimulator.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PinService _pinService;

        public AccountController(AppDbContext context, PinService pinService)
        {
            _context = context;
            _pinService = pinService;
        }

        public async Task<IActionResult> Index()
        {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            var account = await _context.Accounts
            .Include(a => a.Card)
            .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null) return RedirectToAction("InsertCard", "Auth");

            var viewModel = new AccountViewModel
            {
                OwnerName = account.OwnerName,
                Balance = account.Balance,
                CardNumber = account.Card!.CardNumber,
                IsBlocked = account.Card.IsBlocked
            };

            return View(viewModel);
        }

        public IActionResult ChangePin()
        {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            return View(new ChangePinViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ChangePin(ChangePinViewModel model)
        {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            if (!ModelState.IsValid) return View(model);

            try
            {
                await _pinService.ChangePinAsync(accountId.Value, model.CurrentPin, model.NewPin, model.ConfirmPin);
                TempData["Success"] = "PIN успішно змінено";
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