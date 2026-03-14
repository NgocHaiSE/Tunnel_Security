using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TunnelSecurity.Auth.DTOs;
using TunnelSecurity.Data.Auth.Services; // IAuthService

namespace TunnelSecurity.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;
        
        [Authorize(Roles = "StationManager")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest req, CancellationToken ct)
        {
            await _auth.RegisterAsync(req, ct);
            return NoContent();
        }
        [Authorize]
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct = default)
        {
            var resp = await _auth.LoginAsync(req, ct);
            return Ok(resp);
        }


        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh([FromBody] string refreshToken, CancellationToken ct = default)
        {
            var resp = await _auth.RefreshAsync(refreshToken, ct);
            return Ok(resp);
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] string refreshToken, CancellationToken ct = default)
        {
            await _auth.RevokeAsync(refreshToken, ct);
            return NoContent();
        }
    }
}