using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Domain.Exceptions;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Security;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class ConstraintViolationClassificationTests
{
    private readonly RelationalConstraintViolationClassifier _classifier = new();

    [Fact]
    public void SqliteUniqueConstraintViolation_IsClassifiedAsUnique()
    {
        var sqliteEx = new SqliteException("SQLite Error 19: 'UNIQUE constraint failed: AgentPayloadSubmissions.PayloadSubmissionId'.", 19, 2067);
        var dbEx = new DbUpdateException("Error updating database.", sqliteEx);

        var type = _classifier.Classify(dbEx);
        Assert.Equal(RelationalConstraintViolationType.UniqueConstraintViolation, type);
        Assert.True(_classifier.IsUniqueConstraintViolation(dbEx));
    }

    [Fact]
    public void SqliteForeignKeyViolation_IsClassifiedAsForeignKeyViolation()
    {
        var sqliteEx = new SqliteException("SQLite Error 19: 'FOREIGN KEY constraint failed'.", 19, 787);
        var dbEx = new DbUpdateException("Error updating database.", sqliteEx);

        var type = _classifier.Classify(dbEx);
        Assert.Equal(RelationalConstraintViolationType.ForeignKeyViolation, type);
        Assert.False(_classifier.IsUniqueConstraintViolation(dbEx));
    }

    [Fact]
    public void SqliteNotNullViolation_IsClassifiedAsNotNullViolation()
    {
        var sqliteEx = new SqliteException("SQLite Error 19: 'NOT NULL constraint failed'.", 19, 1299);
        var dbEx = new DbUpdateException("Error updating database.", sqliteEx);

        var type = _classifier.Classify(dbEx);
        Assert.Equal(RelationalConstraintViolationType.NotNullViolation, type);
        Assert.False(_classifier.IsUniqueConstraintViolation(dbEx));
    }

    [Fact]
    public void NonUniqueDbUpdateException_IsNotClassifiedAsDuplicate()
    {
        var sqliteEx = new SqliteException("SQLite Error 1: 'no such table: UnknownTable'.", 1);
        var dbEx = new DbUpdateException("Error updating database.", sqliteEx);

        var isUnique = _classifier.IsUniqueConstraintViolation(dbEx);
        Assert.False(isUnique);
    }

    [Fact]
    public async Task ServiceHandlesNonUniqueDbUpdateException_ByPreservingOriginalExceptionWithoutIdempotencyRecovery()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_classifier_test_1"
        });

        // Setup test hook to inject a non-unique DB exception inside the transaction
        service.TestHookBeforeSaveChanges = sub =>
        {
            var nonUniqueSqliteEx = new SqliteException("SQLite Error 19: 'FOREIGN KEY constraint failed'.", 19, 787);
            throw new DbUpdateException("Simulated foreign key failure", nonUniqueSqliteEx);
        };

        var req = new NormalizedWorkbookUploadRequest
        {
            CanonicalSchemaVersion = "1.0",
            DeviceId = pairResp.DeviceId,
            PayloadSubmissionId = "sub_fk_fail_1",
            Nonce = "nonce_fk_fail_123456789",
            ServerImportJobId = Guid.NewGuid(),
            RecordCount = 5,
            NormalizedWorkbook = new NormalizedWorkbook
            {
                WorkbookName = "FKFail.xlsx",
                Sheets = new List<NormalizedSheet>
                {
                    new NormalizedSheet
                    {
                        SheetName = "S1",
                        Rows = new List<NormalizedRow>
                        {
                            new NormalizedRow { RowIndex = 1, Cells = new List<NormalizedCell>() }
                        }
                    }
                }
            }
        };

        // Assert: Throws original DbUpdateException (foreign key violation), NOT falsely resolved as duplicate or wrapped in mismatch exception!
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => service.UploadNormalizedWorkbookAsync(req));
        Assert.Contains("foreign key", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);

        // Prove no submission record was committed
        var count = await db.AgentPayloadSubmissions.CountAsync(s => s.PayloadSubmissionId == "sub_fk_fail_1");
        Assert.Equal(0, count);
    }
}
