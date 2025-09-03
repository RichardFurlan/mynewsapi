namespace MyNewsApi.Domain.Entities;

public class News : BaseEntity
{
    public News(string title,
        string author,
        string description,
        string url,
        string urlToImage,
        string content,
        DateTime publishedAt,
        string sourceId,
        string sourceName,
        string language,
        int? userId
        )
    {
        Title = title;
        Author = author;
        Description = description;
        Url = url;
        UrlToImage = urlToImage;
        Content = content;
        PublishedAt = publishedAt;
        SourceId = sourceId;
        SourceName = sourceName;
        SourceId = sourceId;
        Language = language;
        UserId = userId;
    }

    public string Title { get; private set; }
    public string Author { get; private set; }
    public string Description { get; private set; }
    public string Url { get; private set; }
    public string UrlToImage { get; private set; }
    public string Content { get; private set; }
    public DateTime PublishedAt { get; private set; }
    public string SourceId { get; private set; }
    public string SourceName { get;  private set; }
    public string Language { get; private set; }
    public string Keyword { get; private set; }

    public int? UserId { get; private set; }
    public User User { get; private set; }

    public void SetKeywords(string content)
    {
        Keyword =  content;
    }
}