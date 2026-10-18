using Microsoft.AspNetCore.Mvc;

namespace AtmSimulator.Controllers
{
    public static class ControllerErrorHandlingExtensions
    {
        public static IActionResult InvalidOpAsModelError(this Controller controller, InvalidOperationException ex, object? model)
        {
            controller.ModelState.AddModelError(string.Empty, ex.Message);
            return controller.View(model);
        }
    }
}

