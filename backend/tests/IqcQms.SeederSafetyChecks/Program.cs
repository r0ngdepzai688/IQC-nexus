using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

if (args.Length != 1)
{
    Console.Error.WriteLine("Expected canonical fixture path.");
    return 2;
}

var canonicalFixturePath = Path.GetFullPath(args[0]);
var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"iqc-task-002b-{Guid.NewGuid():N}");
Directory.CreateDirectory(temporaryDirectory);

try
{
    await VerifyMalformedFixturePreventsMigrationAsync(temporaryDirectory);
    await VerifyMissingPasswordMigratesWithoutPersonnelMutationAsync(canonicalFixturePath);
    Console.WriteLine("Seeder startup safety checks passed: malformed fixture blocked migration; missing password skipped personnel mutation.");
    return 0;
}
finally
{
    Directory.Delete(temporaryDirectory, recursive: true);
}

static async Task VerifyMalformedFixturePreventsMigrationAsync(string temporaryDirectory)
{
    var malformedFixturePath = Path.Combine(temporaryDirectory, "malformed-personnel.json");
    await File.WriteAllTextAsync(malformedFixturePath, "[{\"employeeId\":\"invalid\"}]");
    var migrationCalled = false;

    await using var context = CreateContext();
    try
    {
        await UserSeeder.ValidateMigrateAndSyncAsync(
            context,
            malformedFixturePath,
            seedPassword: null,
            NullLogger.Instance,
            () =>
            {
                migrationCalled = true;
                return Task.CompletedTask;
            });
        throw new InvalidOperationException("Malformed fixture was accepted.");
    }
    catch (InvalidDataException)
    {
        if (migrationCalled)
        {
            throw new InvalidOperationException("Migration ran before malformed fixture rejection.");
        }
    }
}

static async Task VerifyMissingPasswordMigratesWithoutPersonnelMutationAsync(string fixturePath)
{
    var migrationCalled = false;
    await using var context = CreateContext();

    await UserSeeder.ValidateMigrateAndSyncAsync(
        context,
        fixturePath,
        seedPassword: null,
        NullLogger.Instance,
        async () =>
        {
            migrationCalled = true;
            await context.Database.EnsureCreatedAsync();
        });

    if (!migrationCalled)
    {
        throw new InvalidOperationException("Migration did not run after valid fixture validation.");
    }
    if (await context.Users.AnyAsync())
    {
        throw new InvalidOperationException("Personnel mutation occurred without a seed password.");
    }
}

static AppDbContext CreateContext()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite("Data Source=:memory:")
        .Options;
    var context = new AppDbContext(options);
    context.Database.OpenConnection();
    return context;
}
