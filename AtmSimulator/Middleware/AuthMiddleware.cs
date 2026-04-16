using AtmSimulator.Data;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (path.StartsWith("/auth"))
            {
                await _next(context);
                return;
            }

            if (path.StartsWith("/admin"))
            {
                await _next(context);
                return;
            }

            if (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/lib"))
            {
                await _next(context);
                return;
            }

            var isAuthenticated = context.Session.GetInt32("AccountId") != null;

            if (!isAuthenticated)
            {
                context.Response.Redirect("/Auth/InsertCard");
                return;
            }

            var cardNumber = context.Session.GetString("AuthenticatedCard");
            if (cardNumber != null) {
                var dbContext = context.RequestServices.GetRequiredService<AppDbContext>();
                var card = await dbContext.Cards.FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
                if (card != null && card.IsBlocked)
                {
                    context.Session.Clear();
                    context.Session.SetString("BlockedMessage", "Вашу картку було заблоковано адміністратором");

                    context.Response.Redirect("/Auth/InsertCard");
                    return;
                }
            }

            await _next(context);
        }
    }
}
