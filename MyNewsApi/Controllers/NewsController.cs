using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyNewsApi.Data;
using MyNewsApi.Models;
using MyNewsApi.Services;

namespace MyNewsApi.Controllers;
[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly NewsService _service;
    private readonly AppDbContext _db;

    public NewsController(NewsService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }
    
    [HttpGet("{keyword}")]
    [ProducesResponseType(typeof(News), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNews(string keyword)
    {
        var articles = await _service.GetNewsAsync(keyword);
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