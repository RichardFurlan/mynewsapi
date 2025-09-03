using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyNewsApi.Application.DTOs;
using MyNewsApi.Application.Interfaces;
using MyNewsApi.Domain.Entities;
using MyNewsApi.Domain.Enums;
using MyNewsApi.Infra.Data;

namespace MyNewsApi.Application.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    #region RegisterAsync
    public async Task<ResultViewModel<User?>> RegisterAsync(RegisterDto userRegister, CancellationToken ct = default)
    {
        var emailAttr = new EmailAddressAttribute();
        if (!emailAttr.IsValid(userRegister.Email))
            return ResultViewModel<User?>.Error("Email inválido");
        if(userRegister.Password.Length < 6)
            return ResultViewModel<User?>.Error("A senha deve ter no mínimo 6 caracteres");
        
        if (await _db.Users.AnyAsync(u => u.Email == userRegister.Email, ct))
        {
            _logger.LogWarning("RegisterAsync: already exists");
            return ResultViewModel<User?>.Error("Já existe um usuário com esse e-mail");
        }

        var normalizedEmail = userRegister.Email.ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            _logger.LogWarning("RegisterAsync: already exists");
            return ResultViewModel<User?>.Error("Já existe um usuário com esse e-mail");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(userRegister.Password);
        var user = new User(userRegister.Email, hash, EnumUserRole.User);
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return ResultViewModel<User?>.Success(user);
    }
    #endregion

    #region LoginAsync
    public async Task<ResultViewModel<string?>> LoginAsync(LoginDto userLogin, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userLogin.Password) || string.IsNullOrEmpty(userLogin.Email))
        {
            _logger.LogWarning("LoginAsync: empty email or password");
            return ResultViewModel<string?>.Error("Preencha os campos e-mail e senha");
        }
        
        var normalizedEmail = userLogin.Email.Trim().ToLowerInvariant();
        var dbUser = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (dbUser == null)
        {
            _logger.LogInformation("LoginAsync: user not found {Email}", normalizedEmail);
            return ResultViewModel<string?>.Error("Usuário não encontrado");
        }
        
        if (!BCrypt.Net.BCrypt.Verify(userLogin.Password, dbUser.PasswordHash))
        {
            _logger.LogInformation("LoginAsync: invalid password for {Email}", normalizedEmail);
            return ResultViewModel<string?>.Error("Senha incorreta");
        }

        var token = GenerateJwtToken(dbUser.Id, dbUser.Email, dbUser.Role.ToString());
        return ResultViewModel<string?>.Success(token);
    }
    

    #endregion

    #region GetUserAsync
    public async Task<ResultViewModel<UserDto?>> GetUserAsync(int? userId, CancellationToken ct = default)
    {
        if (userId == null) return ResultViewModel<UserDto?>.Error("Usuário não autenticado");
        var user = await _db.Users
            .Include(u => u.News) // garante que os dados relacionados venham
            .SingleOrDefaultAsync(u => u.Id == userId, ct);
        
        if (user == null)
            return ResultViewModel<UserDto?>.Error("Usuário não encontrado");

        var newsDto = user.News.Select(
            n => new NewsDto(
                n.Id,
                n.Title,
                n.Description,
                n.Content,
                n.Url,
                n.UrlToImage,
                n.PublishedAt,
                n.SourceName,
                n.UserId)
            ).ToList();


        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.Role,
            newsDto
        );
        
        return ResultViewModel<UserDto?>.Success(userDto);
    }
    #endregion

    #region PromoteToAdminAsync
    public async Task<ResultViewModel> PromoteToAdminAsync(int? userId, CancellationToken ct = default)
    {
        var allow = _config.GetValue<bool>("Auth:AllowAdmin", false);

        if (allow)
        {
            _logger.LogWarning("Self-promotion attempt blocked by configuration for userId={UserId}", userId);
            return ResultViewModel.Error("Não foi possivel definir o usuário como administrador");
        }
        
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            _logger.LogWarning("PromoteToAdmin: user not found (id={UserId})", userId);
            return ResultViewModel.Error("Usuário não encontrado.");
        }

        if (user.Role == EnumUserRole.Admin)
        {
            _logger.LogInformation("PromoteToAdmin: user already admin (id={UserId})", userId);
            return ResultViewModel.Success();
        }
        
        user.DefineAdmin();
        await _db.SaveChangesAsync(ct);
        
        _logger.LogInformation("User {UserId} ({Email}) promoted to Admin.", user.Id, user.Email);
        return ResultViewModel.Success();
    }
    #endregion


    // Privados
    #region GenerateJwtToken
    private string GenerateJwtToken(int userId, string email, string role)
    {
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var expiresHours = DateTime.UtcNow.AddHours(Convert.ToDouble((string?)_config["Jwt:ExpireHours"]));
        var key = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogError("GenerateJwtToken: Jwt:Key is missing");
            throw new InvalidOperationException("JWT key not configured");
        }
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes((string)key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        

        var token = new JwtSecurityToken(
            issuer: issuer, 
            audience: audience, 
            claims: claims,
            expires: expiresHours, 
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();

        var stringToken = tokenHandler.WriteToken(token);

        return stringToken;

    }
    #endregion

    
}