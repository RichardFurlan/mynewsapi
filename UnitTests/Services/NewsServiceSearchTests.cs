using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyNewsApi.Application.Services;
using MyNewsApi.Infra.Clients;
using MyNewsApi.Infra.Data;
using NewsAPI;
using NewsAPI.Models;
using NewsAPI.Constants;
using Xunit;

namespace UnitTests.Services
{
    public class NewsServiceSearchTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static IConfiguration CreateConfig() =>
            new ConfigurationBuilder().AddInMemoryCollection(new[]{ new KeyValuePair<string,string>("NewsApi:ApiKey","FAKE") }!).Build();

        [Fact]
        public async Task SearchSaveAndGetPagedAsync_SavesNewArticles_AndReturnsPaged()
        {
            // Arrange
            var db = CreateDb();
            var mockClient = new Mock<INewsApiClient>();
            var article = new NewsAPI.Models.Article
            {
                Title = "UT Title",
                Author = "UT Author",
                Description = "UT Desc",
                Url = "http://unique.test/article",
                UrlToImage = "http://img",
                Content = "some-key content", 
                PublishedAt = DateTime.UtcNow
            };

            var articlesResult = new ArticlesResult
            {
                Status = Statuses.Ok,
                TotalResults = 1,
                Articles = new List<NewsAPI.Models.Article> { article }
            };

            mockClient.Setup(c => c.GetEverything(It.IsAny<EverythingRequest>())).Returns(articlesResult);

            var logger = new LoggerFactory().CreateLogger<NewsService>();
            var svc = new NewsService(CreateConfig(), db, logger, mockClient.Object);

            // Act
            var result = await svc.SearchSaveAndGetPagedAsync("some-key", userId: 10, page: 1, pageSize: 10, ct: CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue(result.Message);
            result.Data.Should().NotBeNull();
            result.Data.Data.Should().HaveCountGreaterThanOrEqualTo(1);
            (await db.News.CountAsync()).Should().BeGreaterThanOrEqualTo(1);
            var persisted = await db.News.FirstOrDefaultAsync(n => n.Url == "http://unique.test/article");
            persisted.Should().NotBeNull();
            persisted.Title.Should().Be("UT Title");
        }

        [Fact]
        public async Task SearchSaveAndGetPagedAsync_ReturnsError_When_NoItemsFoundAfterSearch()
        {
            // Arrange
            var db = CreateDb();
            var mockClient = new Mock<INewsApiClient>();
            mockClient.Setup(c => c.GetEverything(It.IsAny<EverythingRequest>())).Returns((ArticlesResult)null);

            var svc = new NewsService(CreateConfig(), db, new LoggerFactory().CreateLogger<NewsService>(), mockClient.Object);

            // Act
            var result = await svc.SearchSaveAndGetPagedAsync("", userId: 1, page: 1, pageSize: 10);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }
    }
}
