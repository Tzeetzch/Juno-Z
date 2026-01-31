using Microsoft.Extensions.Logging;
using Moq;
using JunoBank.Web.Services;

namespace JunoBank.Tests.Services;

public class ConsoleEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_LogsEmailDetails()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConsoleEmailService>>();
        var service = new ConsoleEmailService(mockLogger.Object);

        // Act
        var result = await service.SendEmailAsync(
            "test@example.com",
            "Test Subject",
            "<h1>Hello</h1>");

        // Assert
        Assert.True(result);
        
        // Verify logger was called with email details
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test@example.com")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_AlwaysReturnsTrue()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConsoleEmailService>>();
        var service = new ConsoleEmailService(mockLogger.Object);

        // Act
        var result = await service.SendEmailAsync(
            "anyone@anywhere.com",
            "Any Subject",
            "Any body content");

        // Assert - console service always succeeds
        Assert.True(result);
    }

    [Fact]
    public async Task SendEmailAsync_LogsSubject()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConsoleEmailService>>();
        var service = new ConsoleEmailService(mockLogger.Object);
        var subject = "Password Reset Request";

        // Act
        await service.SendEmailAsync("user@test.com", subject, "<p>Reset link</p>");

        // Assert - verify subject was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(subject)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
