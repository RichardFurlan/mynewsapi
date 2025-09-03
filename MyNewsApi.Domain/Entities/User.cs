using MyNewsApi.Domain.Enums;

namespace MyNewsApi.Domain.Entities;

public class User : BaseEntity
{
    public User(string email, string passwordHash, EnumUserRole role = EnumUserRole.User)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public EnumUserRole Role { get; private set; }

    public ICollection<News> News { get; private set; }

    public void DefineAdmin() => Role = EnumUserRole.Admin;
    
}