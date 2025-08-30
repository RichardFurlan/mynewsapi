namespace MyNewsApi.Models;

public class Article : BaseEntity
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime PublishedAt { get; set; }
}