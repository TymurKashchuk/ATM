using AtmSimulator.Data;
using AtmSimulator.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index() {
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
    }
}
