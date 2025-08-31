using System.ComponentModel.DataAnnotations;

namespace MyNewsApi.Models;

public record RegisterDto(    
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(6)] string Password
);