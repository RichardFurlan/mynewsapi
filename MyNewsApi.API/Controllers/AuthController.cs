using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyNewsApi.Application.DTOs;
using MyNewsApi.Application.Interfaces;
using MyNewsApi.Infra.Data;
using MyNewsApi.Infra.Extensions;

namespace MyNewsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(dto, ct);
        if (!result.IsSuccess)
            return BadRequest(result.Message);

        return CreatedAtAction(nameof(Me), new { id = result.Data?.Id }, new UserResponseDto(result.Data.Id, result.Data.Email));
    }
    
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct = default)
    {
        var result = await _auth.LoginAsync(dto, ct);
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        
        return Ok(result);
    }
    
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.GetUserId(); 
        if (userId == null)
            return Unauthorized(ResultViewModel.Error("Usuário não autenticado"));

        var result = await _auth.GetUserAsync(userId, ct);
        if (!result.IsSuccess)
            return BadRequest(result.Message);

        return Ok(result);
    }
    
    [Authorize]
    [HttpPost("admin/promote-me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PromoteMe([FromServices] IConfiguration config, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ResultViewModel.Error("Usuário não autenticado."));
        
        var result = await _auth.PromoteToAdminAsync(userId.Value, ct);

        if (!result.IsSuccess) 
            return BadRequest(result.Message);
        
        return Ok(result);
    }
}