using MyNewsApi.Application.DTOs;
using MyNewsApi.Domain.Entities;

namespace MyNewsApi.Application.Interfaces;

public interface IAuthService
{
    Task<ResultViewModel<User?>> RegisterAsync(RegisterDto userRegister, CancellationToken ct = default);
    Task<ResultViewModel<string?>> LoginAsync(LoginDto userLogin, CancellationToken ct = default);
    Task<ResultViewModel<User?>> GetUserAsync(int? userId, CancellationToken ct = default);
    Task<ResultViewModel> PromoteToAdminAsync(int? userId, CancellationToken ct = default); 
}