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

        public TransactionController(AppDbContext context, WithdrawalService withdrawalService)
        {
            _context = context;
            _withdrawalService = withdrawalService;
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
                model.DispensedCash = dispensed;
                model.CurrentBalance = account.Balance - model.Amount;
                TempData["Success"] = "Withdrawal successful!";
                return View("WithdrawSuccess", model);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }
    }
}
