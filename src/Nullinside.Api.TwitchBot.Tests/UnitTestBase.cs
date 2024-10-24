using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nullinside.Api.Model;

namespace Nullinside.Api.TwitchBot.Tests;

/// <summary>
///     A base class for all unit tests.
/// </summary>
public abstract class UnitTestBase
{
  /// <summary>
  ///     A fake database.
  /// </summary>
  protected INullinsideContext _db;

    [SetUp]
    public virtual void Setup()
    {
        // Create an in-memory database to fake the SQL queries. Note that we generate a random GUID for the name
        // here. If you use the same name more than once you'll get collisions between tests.
        var contextOptions = new DbContextOptionsBuilder<NullinsideContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new NullinsideContext(contextOptions);
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        // Dispose since it has one.
        await _db.DisposeAsync();
    }
}