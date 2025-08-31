namespace MyNewsApi.Models;

public class User : BaseEntity
{
    public string Email { get; private set; }
    public string Password { get; private set; }

    public ICollection<News> News { get; private set; }
}