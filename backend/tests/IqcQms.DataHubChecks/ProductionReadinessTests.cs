using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IqcQms.Api.Security;
using IqcQms.Application.DataPlatform;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.DataPlatform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class ProductionReadinessTests
{
    private static AppDbContext CreateMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task WorkQueue_EnqueueAndAcquireLease_Succeeds()
    {
        using var db = CreateMemoryDb(Guid.NewGuid().ToString());
        var queue = new EfImportWorkQueue(db, NullLogger<EfImportWorkQueue>.Instance);

        var req = new EnqueueCommitWorkRequest("job-101", "key-101", 1, "user-1", "corr-101");
        var workItem = await queue.EnqueueCommitAsync(req);

        Assert.NotNull(workItem);
        Assert.Equal("Pending", workItem.State);

        var leases = await queue.AcquireLeasesAsync("worker-1", batchSize: 5);
        Assert.Single(leases);
        Assert.Equal("Leased", leases[0].State);
        Assert.Equal("worker-1", leases[0].LeaseOwner);
    }

    [Fact]
    public async Task WorkQueue_PoisonDetection_AfterMaxAttempts()
    {
        using var db = CreateMemoryDb(Guid.NewGuid().ToString());
        var queue = new EfImportWorkQueue(db, NullLogger<EfImportWorkQueue>.Instance);

        var req = new EnqueueCommitWorkRequest("job-102", "key-102", 1, "user-1", "corr-102");
        var workItem = await queue.EnqueueCommitAsync(req);

        await queue.FailWorkItemAsync(workItem.WorkItemId, "worker-1", "ERR1", "Fail 1");
        await queue.FailWorkItemAsync(workItem.WorkItemId, "worker-1", "ERR2", "Fail 2");
        await queue.FailWorkItemAsync(workItem.WorkItemId, "worker-1", "ERR3", "Fail 3");

        var updated = await db.PersistentImportWorkItems.FindAsync(workItem.WorkItemId);
        Assert.NotNull(updated);
        Assert.Equal("Poison", updated.State);
    }

    [Fact]
    public async Task HealthChecks_ReadinessAndOperational_ReturnsHealthy()
    {
        using var db = CreateMemoryDb(Guid.NewGuid().ToString());
        var readyCheck = new ImportReadinessHealthCheck(db);
        var opCheck = new ImportOperationalHealthCheck(db);

        var readyResult = await readyCheck.CheckHealthAsync(new HealthCheckContext());
        var opResult = await opCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, readyResult.Status);
        Assert.Equal(HealthStatus.Healthy, opResult.Status);
    }

    [Fact]
    public async Task CsvFormulaNeutralization_PrefixesQuotesOnFormulaCharacters()
    {
        var csvContent = "Part Number,Qty,Result\n=1+1,10,PASS\n+SUM(A1:A2),20,PASS\n-5,30,PASS\n@calc,40,PASS";
        var bytes = Encoding.UTF8.GetBytes(csvContent);

        var descriptor = new DataSourceDescriptor(DataSourceProviderKind.Csv, "test.csv", "test.csv", "text/csv", bytes.Length);
        var provider = new CsvDataSourceProvider();
        var context = new DataSourceProviderContext(descriptor, new MemoryStream(bytes), ImportPlatformLimits.Default);

        var workbook = await provider.NormalizeAsync(context);
        var worksheet = workbook.Worksheets[0];

        Assert.Equal("'=1+1", worksheet.Rows[1].Cells[0].StringValue);
        Assert.Equal("'+SUM(A1:A2)", worksheet.Rows[2].Cells[0].StringValue);
        Assert.Equal("'-5", worksheet.Rows[3].Cells[0].StringValue);
        Assert.Equal("'@calc", worksheet.Rows[4].Cells[0].StringValue);
    }
}
