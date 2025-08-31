using System.ComponentModel.DataAnnotations;

namespace MyNewsApi.Models;

public record RegisterDto(string Email,string Password);