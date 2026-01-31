using JunoBank.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace JunoBank.Tests.Helpers;

/// <summary>
/// Base class for tests that need an in-memory database.
/// </summary>
public abstract class DatabaseTestBase : IDisposable
{
    protected readonly AppDbContext Db;

    protected DatabaseTestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Db = new AppDbContext(options);
    }

    /// <summary>
    /// Create a mock logger for any type.
    /// </summary>
    protected static ILogger<T> CreateLogger<T>() => Mock.Of<ILogger<T>>();

    public void Dispose()
    {
        Db.Dispose();
    }
}
