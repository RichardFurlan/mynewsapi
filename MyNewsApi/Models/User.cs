namespace MyNewsApi.Models;

public class User : BaseEntity
{
    public User(string email, string passwordHash)
    {
        Email = email;
        PasswordHash = passwordHash;
    }

    public string Email { get; private set; }
    public string PasswordHash { get; private set; }

    public ICollection<News> News { get; private set; }
}