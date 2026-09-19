using FruitShop.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FruitShop.Api.Tests;

internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<FruitShopDbContext> _options;

    private SqliteTestDatabase(SqliteConnection connection, DbContextOptions<FruitShopDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static async Task<SqliteTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<FruitShopDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new FruitShopDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return new SqliteTestDatabase(connection, options);
    }

    public FruitShopDbContext CreateContext() => new(_options);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}