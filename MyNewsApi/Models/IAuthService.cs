namespace MyNewsApi.Models;

public interface IAuthService
{
    Task<User?> RegisterAsync(RegisterDto userRegister, CancellationToken ct = default);
    Task<string?>? LoginAsync(LoginDto userLogin, CancellationToken ct = default);
    Task<User?> GetUserAsync(string userId, CancellationToken ct = default);
}