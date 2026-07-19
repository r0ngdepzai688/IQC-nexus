using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace IqcQms.SeederSafetyChecks;

public sealed class SQLiteProviderTests
{
    private static readonly Version MinimumPatchedEngineVersion = new(3, 50, 2);
    private readonly ITestOutputHelper _output;

    public SQLiteProviderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task InMemoryProviderLoadsPatchedSQLiteEngine()
    {
        await using var context = CreateContext("Data Source=:memory:");
        await context.Database.OpenConnectionAsync();

        var version = await ReadEngineVersionAsync(context);

        _output.WriteLine("In-memory SQLite engine: {0}", version);
        Assert.True(version >= MinimumPatchedEngineVersion, $"SQLite {version} is older than patched baseline {MinimumPatchedEngineVersion}.");
    }

    [Fact]
    public async Task FileBackedProviderLoadsPatchedSQLiteEngine()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"iqc-sqlite-provider-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var databasePath = Path.Combine(temporaryDirectory, "provider-check.db");
            Version version;
            await using (var context = CreateContext($"Data Source={databasePath};Pooling=False"))
            {
                await context.Database.OpenConnectionAsync();
                version = await ReadEngineVersionAsync(context);
            }

            _output.WriteLine("File-backed SQLite engine: {0}", version);
            Assert.True(File.Exists(databasePath), "SQLite did not create the temporary file-backed database.");
            Assert.True(version >= MinimumPatchedEngineVersion, $"SQLite {version} is older than patched baseline {MinimumPatchedEngineVersion}.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static AppDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Version> ReadEngineVersionAsync(AppDbContext context)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT sqlite_version();";
        var result = await command.ExecuteScalarAsync();
        return Version.Parse(Assert.IsType<string>(result));
    }
}
