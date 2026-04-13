using AtmSimulator.Data;
using AtmSimulator.Services;
using AtmSimulator.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AtmSimulator.Controllers
{
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly WithdrawalService _withdrawalService;
        private readonly DepositService _depositService;
        private readonly TransferService _transferService;

        public TransactionController(AppDbContext context, WithdrawalService withdrawalService, DepositService depositService, TransferService transferService)
        {
            _context = context;
            _withdrawalService = withdrawalService;
            _depositService = depositService;
            _transferService = transferService;
        }

        public async Task<IActionResult> Withdraw() {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            var account = await _context.Accounts.FindAsync(accountId);
            return View(new WithdrawalViewModel { CurrentBalance = account!.Balance });
        }

        [HttpPost]
        public async Task<IActionResult> Withdraw(WithdrawalViewModel model) {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            var account = await _context.Accounts.FindAsync(accountId);
            model.CurrentBalance = account!.Balance;

            if (!ModelState.IsValid) return View(model);

            try
            {
                var dispensed = await _withdrawalService.WithdrawAsync(accountId.Value, model.Amount);
                await _context.Entry(account).ReloadAsync();

                model.DispensedCash = dispensed;
                model.CurrentBalance = account.Balance;

                return View("WithdrawSuccess", model);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Deposit()
        {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            var account = await _context.Accounts.FindAsync(accountId);
            return View(new DepositViewModel { CurrentBalance = account!.Balance});
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(DepositViewModel model) {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            var account = await _context.Accounts.FindAsync(accountId);
            model.CurrentBalance = account!.Balance;

            if (!ModelState.IsValid) return View(model);

            try
            {
                await _depositService.DepositAsync(accountId.Value, model.Amount);
                await _context.Entry(account).ReloadAsync();

                TempData["Success"] = $"Успішно внесено {model.Amount:N2} ₴";
                return RedirectToAction("Index", "Account");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Transfer()
        {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            var account = await _context.Accounts.FindAsync(accountId);
            return View(new TransferViewModel { CurrentBalance = account!.Balance });
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(TransferViewModel model)
        {
            var accountId = HttpContext.Session.GetInt32("AccountId");
            if (accountId == null) return RedirectToAction("InsertCard", "Auth");

            var account = await _context.Accounts.FindAsync(accountId);
            model.CurrentBalance = account!.Balance;

            if (!ModelState.IsValid) return View(model);

            try
            {
                await _transferService.TransferAsync(accountId.Value, model.RecipientCardNumber, model.Amount);
                await _context.Entry(account).ReloadAsync();

                TempData["Success"] = $"Успішно переведено {model.Amount:N2} ₴";
                return RedirectToAction("Index", "Account");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }
    }
}
