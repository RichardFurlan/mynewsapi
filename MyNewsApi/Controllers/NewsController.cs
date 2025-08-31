using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyNewsApi.Data;
using MyNewsApi.Models;
using MyNewsApi.Services;

namespace MyNewsApi.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly NewsService _service;
    private readonly AppDbContext _db;

    public NewsController(NewsService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }
    
    [Authorize]
    [HttpGet("{keyword}")]
    [ProducesResponseType(typeof(News), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(News), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(News), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNews(string keyword)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();
        var articles = await _service.GetNewsAsync(keyword, userId);
        
        if(!articles.Any())
            return NotFound();
        
        return Ok(articles);
    }
    
    
    [HttpGet("all")]
    [ProducesResponseType(typeof(News), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var articles = await _db.News.ToListAsync();
        return Ok(articles);
    }
}