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

            await _next(context);
        }
    }
}
