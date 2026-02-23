using Microsoft.AspNetCore.Mvc;

namespace pod.keycloak
{
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx, SessionService sessionService)
        {
            var sid = ctx.Request.Cookies["rapidisimo_session"];

            if (!string.IsNullOrEmpty(sid))
            {
                var session = await sessionService.GetSession(sid);
                if (session != null)
                    ctx.Items["session"] = session;
            }

            await _next(ctx);
        }
    }
}
