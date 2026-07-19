using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.SeederSafetyChecks;

public sealed class SeederSafetyTests
{
    [Fact]
    public async Task MalformedFixturePreventsMigration()
    {
        var temporaryDirectory = CreateTemporaryDirectory();
        try
        {
            var malformedFixturePath = Path.Combine(temporaryDirectory, "malformed-personnel.json");
            await File.WriteAllTextAsync(malformedFixturePath, "[{\"employeeId\":\"invalid\"}]");
            var migrationCalled = false;

            await using var context = CreateContext();
            await Assert.ThrowsAsync<InvalidDataException>(() => UserSeeder.ValidateMigrateAndSyncAsync(
                context,
                malformedFixturePath,
                seedPassword: null,
                NullLogger.Instance,
                () =>
                {
                    migrationCalled = true;
                    return Task.CompletedTask;
                }));

            Assert.False(migrationCalled, "Migration ran before malformed fixture rejection.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MissingPasswordMigratesWithoutPersonnelMutation()
    {
        var migrationCalled = false;
        await using var context = CreateContext();

        await UserSeeder.ValidateMigrateAndSyncAsync(
            context,
            FindCanonicalFixture(),
            seedPassword: null,
            NullLogger.Instance,
            async () =>
            {
                migrationCalled = true;
                await context.Database.EnsureCreatedAsync();
            });

        Assert.True(migrationCalled, "Migration did not run after valid fixture validation.");
        Assert.False(await context.Users.AnyAsync(), "Personnel mutation occurred without a seed password.");
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var context = new AppDbContext(options);
        context.Database.OpenConnection();
        return context;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"iqc-seeder-safety-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindCanonicalFixture()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "fixtures", "personnel.synthetic.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Could not locate fixtures/personnel.synthetic.json from the test output directory.");
    }
}
