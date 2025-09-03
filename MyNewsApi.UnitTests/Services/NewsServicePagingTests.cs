using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyNewsApi.Application.Services;
using MyNewsApi.Domain.Entities;
using MyNewsApi.Infra.Data;
using Xunit;

namespace UnitTests.Services
{
    public class NewsServicePagingTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static IConfiguration CreateConfig() =>
            new ConfigurationBuilder().AddInMemoryCollection(new[]{ new KeyValuePair<string,string>("NewsApi:ApiKey","FAKE") }).Build();

        [Fact]
        public async Task GetNewsPagedAsync_Returns_PagedResults()
        {
            // Arrange
            var db = CreateDb();
            
            for (int i = 1; i <= 25; i++)
            {
                var n = new News(
                    title: $"Title {i}",
                    author: "Author",
                    description: "Desc",
                    url: $"http://u{i}",
                    urlToImage: "img",
                    content: "content",
                    publishedAt: DateTime.UtcNow.AddMinutes(-i),
                    sourceName: "source",
                    sourceId: "sid",
                    language: "en",
                    userId: i % 2 == 0 ? 2 : 1
                );
                
                try
                {
                    var mi = n.GetType().GetMethod("SetKeywords");
                    if (mi != null)
                    {
                        mi.Invoke(n, new object?[] { "seed,test" });
                    }
                    else
                    {
                        var prop = n.GetType().GetProperty("Keywords") ?? n.GetType().GetProperty("Keyword");
                        if (prop != null && prop.CanWrite)
                            prop.SetValue(n, "seed,test");
                    }
                }
                catch
                {
                    // se algo falhar aqui, não interrompe o teste — o erro real aparecerá no SaveChanges
                }

                db.News.Add(n);
            }

            await db.SaveChangesAsync();

            var logger = new LoggerFactory().CreateLogger<NewsService>();
            var svc = new NewsService(CreateConfig(), db, logger, client: null); // usa null porque não precisa do client aqui

            // Act
            var resPage1 = await svc.GetNewsPagedAsync(page: 1, pageSize: 10, userId: null, ct: CancellationToken.None);
            var resPage3 = await svc.GetNewsPagedAsync(page: 3, pageSize: 10, userId: null, ct: CancellationToken.None);

            // Assert
            resPage1.IsSuccess.Should().BeTrue(resPage1.Message);
            resPage1.Data?.Data.Should().HaveCount(10);
            resPage1.Data?.Total.Should().Be(25);

            resPage3.Data?.Data.Should().HaveCount(5);
        }
    }
}
