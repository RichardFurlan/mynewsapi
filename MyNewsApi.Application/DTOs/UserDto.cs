using MyNewsApi.Domain.Enums;

namespace MyNewsApi.Application.DTOs;

public record UserDto(int Id, string Email, EnumUserRole Role, List<NewsDto> News);