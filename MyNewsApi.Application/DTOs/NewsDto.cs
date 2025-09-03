namespace MyNewsApi.Application.DTOs;

public record NewsDto(    
    int? Id,
    string Title,
    string Description,
    string Content,
    string Url,
    string UrlToImage,
    DateTime PublishedAt,
    string SourceName,
    int? UserId
);