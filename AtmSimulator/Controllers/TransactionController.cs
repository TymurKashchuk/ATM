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
        private readonly TransactionService _transactionService;

        public TransactionController(AppDbContext context, WithdrawalService withdrawalService, DepositService depositService,
            TransferService transferService, TransactionService transactionService)
        {
            _context = context;
            _withdrawalService = withdrawalService;
            _depositService = depositService;
            _transferService = transferService;
            _transactionService = transactionService;
        }

        public async Task<IActionResult> Withdraw()
        {
            var accountId = (int)HttpContext.Session.GetInt32("AccountId")!;

            var account = await _context.Accounts.FindAsync(accountId);
            return View(new WithdrawalViewModel { CurrentBalance = account!.Balance });
        }

        [HttpPost]
        public async Task<IActionResult> Withdraw(WithdrawalViewModel model)
        {
            var accountId = (int)HttpContext.Session.GetInt32("AccountId")!;

            var account = await _context.Accounts.FindAsync(accountId);
            model.CurrentBalance = account!.Balance;

            if (!ModelState.IsValid) return View(model);

            try
            {
                var dispensed = await _withdrawalService.WithdrawAsync(accountId, model.Amount);
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
            var accountId = (int)HttpContext.Session.GetInt32("AccountId")!;

            var account = await _context.Accounts.FindAsync(accountId);
            return View(new DepositViewModel { CurrentBalance = account!.Balance });
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(DepositViewModel model)
        {
            var accountId = (int)HttpContext.Session.GetInt32("AccountId")!;

            var account = await _context.Accounts.FindAsync(accountId);
            model.CurrentBalance = account!.Balance;

            if (!ModelState.IsValid) return View(model);

            try
            {
                await _depositService.DepositAsync(accountId, model.Amount);
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
            var accountId = (int)HttpContext.Session.GetInt32("AccountId")!;

            var account = await _context.Accounts.FindAsync(accountId);
            return View(new TransferViewModel { CurrentBalance = account!.Balance });
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(TransferViewModel model)
        {
            var accountId = (int)HttpContext.Session.GetInt32("AccountId")!;

            var account = await _context.Accounts.FindAsync(accountId);
            model.CurrentBalance = account!.Balance;

            if (!ModelState.IsValid) return View(model);

            try
            {
                await _transferService.TransferAsync(accountId, model.RecipientCardNumber, model.Amount);
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

        public async Task<IActionResult> History(int page = 1)
        {
            var accountId = (int)HttpContext.Session.GetInt32("AccountId")!;

            const int pageSize = 10;
            var transactions = await _transactionService.GetHistoryAsync(accountId, page, pageSize);
            var totalCount = await _transactionService.GetTotalCountAsync(accountId);

            var viewModel = new TransactionHistoryViewModel
            {
                Transactions = transactions,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                TotalCount = totalCount
            };

            return View(viewModel);
        }
    }
}