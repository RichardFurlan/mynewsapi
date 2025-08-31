using System.ComponentModel.DataAnnotations;

namespace MyNewsApi.Models;

public record LoginDto(string Email, string Password);