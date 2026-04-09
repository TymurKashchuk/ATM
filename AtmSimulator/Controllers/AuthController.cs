using AtmSimulator.Services;
using AtmSimulator.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AtmSimulator.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        public IActionResult InsertCard() => View(new CardViewModel());

        [HttpPost]
        public async Task<IActionResult> InsertCard(CardViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var card = await _authService.FindCardAsync(model.CardNumber);

            if (card == null)
            {
                ModelState.AddModelError("", "Картку не знайдено");
                return View(model);
            }

            if (card.IsBlocked)
            {
                ModelState.AddModelError("", "Картку заблоковано");
                return View(model);
            }

            HttpContext.Session.SetString("CardNumber", card.CardNumber);
            return RedirectToAction("EnterPin");
        }

        public IActionResult EnterPin()
        {
            if (HttpContext.Session.GetString("CardNumber") == null)
                return RedirectToAction("InsertCard");

            return View(new PinViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> EnterPin(PinViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var cardNumber = HttpContext.Session.GetString("CardNumber");
            var card = await _authService.FindCardAsync(cardNumber!);

            if (card == null) return RedirectToAction("InsertCard");

            var isValid = await _authService.ValidatePinAsync(card, model.Pin);

            if (!isValid)
            {
                if (card.IsBlocked)
                {
                    HttpContext.Session.Clear();
                    ModelState.AddModelError("", "Картку заблоковано після 3 невірних спроб");
                    return View(model);
                }

                ModelState.AddModelError("", $"Невірний PIN. Залишилось спроб: {_authService.GetRemainingAttempts(card)}");
                return View(model);
            }

            HttpContext.Session.SetString("AuthenticatedCard", card.CardNumber);
            HttpContext.Session.SetInt32("AccountId", card.Account!.Id);
            return RedirectToAction("Index", "Account");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("InsertCard");
        }
    }
}