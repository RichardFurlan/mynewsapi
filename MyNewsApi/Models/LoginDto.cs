using System.ComponentModel.DataAnnotations;

namespace MyNewsApi.Models;

public record LoginDto(    
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password
);