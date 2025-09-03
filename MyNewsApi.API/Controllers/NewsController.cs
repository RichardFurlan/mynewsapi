using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyNewsApi.Application.DTOs;
using MyNewsApi.Application.Services;
using MyNewsApi.Infra.Data;
using MyNewsApi.Infra.Extensions;

namespace MyNewsApi.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly NewsService _service;

    public NewsController(NewsService service)
    {
        _service = service;
    }
    
    [HttpGet("search")]
    [ProducesResponseType(typeof(ResultViewModel<PagedResult<NewsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultViewModel<PagedResult<NewsDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResultViewModel<PagedResult<NewsDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchNews(
        string keyword, 
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ResultViewModel.Error("Usuário não autenticado"));
        var result = await _service.SearchSaveAndGetPagedAsync(keyword, userId.Value, page, pageSize, ct);
        
        if(!result.IsSuccess)
            return BadRequest(result.Message);
        
        return Ok(result);
    }
    
    [AllowAnonymous]
    [HttpGet("all")]
    [ProducesResponseType(typeof(ResultViewModel<PagedResult<NewsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResultViewModel<PagedResult<NewsDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page <= 0 || pageSize <= 0)
            return BadRequest(ResultViewModel<PagedResult<NewsDto>>.Error("page and pageSize devem ser maior que zero"));
        var result = await _service.GetNewsPagedAsync(page, pageSize, null,ct);
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        
        return Ok(result);
    }
    
    [HttpGet("me")]
    [ProducesResponseType(typeof(ResultViewModel<PagedResult<NewsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyNews([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue) return Unauthorized(ResultViewModel.Error("Usuário não autenticado"));
        
        var result = await _service.GetNewsPagedAsync(page, pageSize, userId.Value, ct);
        
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        
        return Ok(result);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(ResultViewModel<PagedResult<NewsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetNewsByUserId([FromRoute] int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetNewsPagedAsync(page, pageSize, userId);
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        
        return Ok(result);
    }
}