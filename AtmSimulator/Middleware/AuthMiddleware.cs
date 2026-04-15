namespace AtmSimulator.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly string[] PublicRoutes = {
            "/Auth/InsertCard",
            "/Auth/EnterPin",
            "/Auth/Logout"
        };

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            var isPublic = PublicRoutes.Any(r =>
                path.StartsWith(r, StringComparison.OrdinalIgnoreCase));

            var isAuthenticated = context.Session.GetInt32("AccountId") != null;

            if (!isPublic && !isAuthenticated)
            {
                context.Response.Redirect("/Auth/InsertCard");
                return;
            }

            await _next(context);
        }
    }
}
