using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyNewsApi.Data;
using MyNewsApi.Models;

namespace MyNewsApi.Services;

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
    
    public async Task<User?> RegisterAsync(RegisterDto userRegister, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == userRegister.Email, ct))
        {
            _logger.LogWarning("RegisterAsync: empty email or password");
            return null;
        }

        var normalizedEmail = userRegister.Email.ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct))
        {
            _logger.LogWarning("RegisterAsync: already exists");
            return null;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(userRegister.Password);
        var user = new User(userRegister.Email, hash);
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<string?>? LoginAsync(LoginDto userLogin, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userLogin.Password) || string.IsNullOrEmpty(userLogin.Email))
        {
            _logger.LogWarning("LoginAsync: empty email or password");
            return null;
        }
        
        var normalizedEmail = userLogin.Email.Trim().ToLowerInvariant();
        var dbUser = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        
        if (dbUser == null)
        {
            _logger.LogInformation("LoginAsync: user not found {Email}", normalizedEmail);
            return null;
        }
        
        if (!BCrypt.Net.BCrypt.Verify(userLogin.Password, dbUser.PasswordHash))
        {
            _logger.LogInformation("LoginAsync: invalid password for {Email}", normalizedEmail);
            return null;
        }

        var token = GenerateJwtToken(dbUser.Id, dbUser.Email);
        return token;
    }

    public async Task<User?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var id))
            return null;
        return await _db.Users.FindAsync(new object[] { id }, ct);
    }
    
    
    public string GenerateJwtToken(int userId, string email)
    {
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var expiresHours = DateTime.UtcNow.AddHours(Convert.ToDouble(_config["Jwt:ExpireHours"]));
        var key = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogError("GenerateJwtToken: Jwt:Key is missing");
            throw new InvalidOperationException("JWT key not configured");
        }
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
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
}