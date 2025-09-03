using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyNewsApi.Application.DTOs;
using MyNewsApi.Application.Services;
using MyNewsApi.Domain.Entities;
using MyNewsApi.Domain.Enums;
using MyNewsApi.Infra.Data;
using Xunit;

namespace UnitTests.Services
{
    public class AuthServiceTests
    {
        private static AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static IConfiguration CreateJwtConfiguration(string key = "TEST_TEST_TEST_TEST_TEST_TEST_TEST_32")
        {
            var dict = new Dictionary<string, string>
            {
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = "MyNewsApi",
                ["Jwt:Audience"] = "MyNewsApiUsers",
                ["Jwt:ExpireHours"] = "24",
                // controle para PromoteToAdminAsync
                ["Auth:AllowAdmin"] = "false"
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [Fact]
        public async Task RegisterAsync_Should_CreateUser_When_ValidInput()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var config = CreateJwtConfiguration();
            var logger = new LoggerFactory().CreateLogger<AuthService>();
            var svc = new AuthService(db, config, logger);

            var dto = new RegisterDto("user@example.com","123456");

            // Act
            var result = await svc.RegisterAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            (await db.Users.CountAsync()).Should().Be(1);
            (await db.Users.SingleAsync()).Email.Should().Be("user@example.com");
        }

        [Fact]
        public async Task RegisterAsync_Should_ReturnError_When_InvalidEmail()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var svc = new AuthService(db, CreateJwtConfiguration(), new LoggerFactory().CreateLogger<AuthService>());

            var dto = new RegisterDto("invalid-email", "123456" );

            // Act
            var result = await svc.RegisterAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Email inválido");
        }

        [Fact]
        public async Task RegisterAsync_Should_ReturnError_When_PasswordTooShort()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var svc = new AuthService(db, CreateJwtConfiguration(), new LoggerFactory().CreateLogger<AuthService>());

            var dto = new RegisterDto("user2@example.com", "123");

            // Act
            var result = await svc.RegisterAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("mínimo");
        }

        [Fact]
        public async Task RegisterAsync_Should_ReturnError_When_EmailExists()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var logger = new LoggerFactory().CreateLogger<AuthService>();
            var hashed = BCrypt.Net.BCrypt.HashPassword("123456");
            var existing = new User("user3@example.com", hashed, EnumUserRole.User);
            db.Users.Add(existing);
            await db.SaveChangesAsync();

            var svc = new AuthService(db, CreateJwtConfiguration(), logger);
            var dto = new RegisterDto("user3@example.com","123456" );

            // Act
            var result = await svc.RegisterAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Já existe");
        }

        [Fact]
        public async Task LoginAsync_Should_ReturnToken_When_CorrectCredentials()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var config = CreateJwtConfiguration();
            var logger = new LoggerFactory().CreateLogger<AuthService>();
            
            var hashed = BCrypt.Net.BCrypt.HashPassword("123456");
            var user = new User("login@example.com", hashed, EnumUserRole.User);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var svc = new AuthService(db, config, logger);

            var loginDto = new LoginDto("login@example.com", "123456");
           
            // Act
            var result = await svc.LoginAsync(loginDto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();

            // validate token structure
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            handler.CanReadToken(result.Data!).Should().BeTrue();
        }

        [Fact]
        public async Task LoginAsync_Should_ReturnError_When_UserNotFound()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var svc = new AuthService(db, CreateJwtConfiguration(), new LoggerFactory().CreateLogger<AuthService>());

            var loginDto = new LoginDto("notfound@example.com", "123456");
            
            // Act
            var result = await svc.LoginAsync(loginDto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Usuário não encontrado");
        }

        [Fact]
        public async Task LoginAsync_Should_ReturnError_When_WrongPassword()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var config = CreateJwtConfiguration();
            var logger = new LoggerFactory().CreateLogger<AuthService>();

            var hashed = BCrypt.Net.BCrypt.HashPassword("rightpassword");
            var user = new User("u4@example.com", hashed, EnumUserRole.User);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var svc = new AuthService(db, config, logger);
            
            // Act
            var result = await svc.LoginAsync(new LoginDto("u4@example.com","wrong"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Senha incorreta");
        }

        [Fact]
        public async Task PromoteToAdminAsync_When_AllowAdminTrue_Should_ReturnError()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var dict = new Dictionary<string,string> { ["Auth:AllowAdmin"] = "true" };
            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            var logger = new LoggerFactory().CreateLogger<AuthService>();

            var user = new User("p1@example.com", BCrypt.Net.BCrypt.HashPassword("123456"), EnumUserRole.User);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var svc = new AuthService(db, config, logger);
            
            // Act
            var result = await svc.PromoteToAdminAsync(user.Id, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task PromoteToAdminAsync_When_AllowAdminFalse_Should_Promote()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var dict = new Dictionary<string,string> { ["Auth:AllowAdmin"] = "false" };
            var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            var logger = new LoggerFactory().CreateLogger<AuthService>();

            var user = new User("p2@example.com", BCrypt.Net.BCrypt.HashPassword("123456"), EnumUserRole.User);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var svc = new AuthService(db, config, logger);
            
            // Act
            var result = await svc.PromoteToAdminAsync(user.Id, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var reloaded = await db.Users.FindAsync(user.Id);
            reloaded!.Role.Should().Be(EnumUserRole.Admin);
        }
    }
}
