using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyNewsApi.Models;

namespace MyNewsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(dto, ct);
        if (result == null)
        {
            return Conflict(new { message = "Email already in use or invalid data" });
        }
        return Ok(result);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct = default)
    {
        var token = await _auth.LoginAsync(dto, ct);
        if (token == null) return Unauthorized(new { message = "Invalid credentials" });
        return Ok(new { token });
    }
    
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var id)) return Unauthorized();
        var user = await _auth.GetUserAsync(idClaim);
        if (user == null) return NotFound();
        return Ok(new { user.Id, user.Email });
    }
    
}