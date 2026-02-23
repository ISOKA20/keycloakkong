using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace pod.keycloak.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly KeycloakService _kc;
        private readonly SessionService _session;

        public AuthController(KeycloakService kc, SessionService session)
        {
            _kc = kc;
            _session = session;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest req)
        {
            var token = await _kc.Login(req.Email, req.Password);

            var access = token.RootElement.GetProperty("access_token").GetString();
            var refresh = token.RootElement.GetProperty("refresh_token").GetString();

            var sessionId = Guid.NewGuid().ToString();

            await _session.CreateSession(new SessionModel
            {
                SessionId = sessionId,
                AccessToken = access!,
                RefreshToken = refresh!,
                Username = req.Email,
                Expiration = DateTime.UtcNow.AddMinutes(10)
            });

            Response.Cookies.Append("rapidisimo_session", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,     // ¡VITAL! Si es true y no usas HTTPS, no se guarda
                SameSite = SameSiteMode.Lax, // Permite que la cookie viaje entre puertos del mismo localhost
                Path = "/",         // Para que sea válida en /api y /auth
                Expires = DateTimeOffset.UtcNow.AddMinutes(60)
            });

            return Ok(new { message = "login ok" });
        }
    }
}
