using System.Data;
using IqcQms.ClientAgent.Application.Config;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Queue;

public class LocalJobItem
{
    public string LocalJobId { get; set; } = Guid.NewGuid().ToString("N");
    public Guid ServerJobId { get; set; }
    public string WorkType { get; set; } = "SyntheticNormalization";
    public string State { get; set; } = "Pending"; // Pending, Leased, Completed, Failed, Poisoned
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public DateTime AvailableAtUtc { get; set; } = DateTime.UtcNow;
    public string? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? LastErrorCode { get; set; }
    public string PayloadReference { get; set; } = string.Empty;
}

public interface ILocalAgentQueue
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<LocalJobItem> EnqueueJobAsync(Guid serverJobId, string workType, string payloadReference, CancellationToken cancellationToken = default);
    Task<LocalJobItem?> AcquireNextLeaseAsync(string leaseOwner, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task CompleteJobAsync(string localJobId, CancellationToken cancellationToken = default);
    Task FailJobAsync(string localJobId, string errorCode, TimeSpan backoff, CancellationToken cancellationToken = default);
    Task RecoverStaleLeasesAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}

public class SqliteLocalAgentQueue : ILocalAgentQueue
{
    private readonly string _dbPath;
    private readonly AgentOptions _options;
    private readonly ILogger<SqliteLocalAgentQueue> _logger;

    public SqliteLocalAgentQueue(string dbDirectory, AgentOptions options, ILogger<SqliteLocalAgentQueue> logger)
    {
        Directory.CreateDirectory(dbDirectory);
        _dbPath = Path.Combine(dbDirectory, "agent_queue.db");
        _options = options;
        _logger = logger;
    }

    private string ConnectionString => $"Data Source={_dbPath}";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = @"
            CREATE TABLE IF NOT EXISTS AgentLocalQueue (
                LocalJobId TEXT PRIMARY KEY,
                ServerJobId TEXT NOT NULL,
                WorkType TEXT NOT NULL,
                State TEXT NOT NULL,
                AttemptCount INTEGER NOT NULL,
                MaxAttempts INTEGER NOT NULL,
                AvailableAtUtc TEXT NOT NULL,
                LeaseOwner TEXT,
                LeaseExpiresAtUtc TEXT,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL,
                LastErrorCode TEXT,
                PayloadReference TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Queue_State_Available ON AgentLocalQueue(State, AvailableAtUtc);
            CREATE INDEX IF NOT EXISTS IX_Queue_ServerJobId ON AgentLocalQueue(ServerJobId);
        ";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        await RecoverStaleLeasesAsync(cancellationToken);
    }

    public async Task<LocalJobItem> EnqueueJobAsync(Guid serverJobId, string workType, string payloadReference, CancellationToken cancellationToken = default)
    {
        // Enforce allowed root validation on payload reference
        if (!string.IsNullOrWhiteSpace(payloadReference) && !_options.IsPathAllowed(payloadReference))
        {
            _logger.LogWarning("Rejected payload reference path {Path} outside allowed roots.", payloadReference);
            throw new InvalidOperationException($"Payload reference path '{payloadReference}' is not within configured allowed roots.");
        }

        var item = new LocalJobItem
        {
            LocalJobId = Guid.NewGuid().ToString("N"),
            ServerJobId = serverJobId,
            WorkType = workType,
            State = "Pending",
            AttemptCount = 0,
            MaxAttempts = 5,
            AvailableAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            PayloadReference = payloadReference ?? string.Empty
        };

        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO AgentLocalQueue (
                LocalJobId, ServerJobId, WorkType, State, AttemptCount, MaxAttempts, AvailableAtUtc, CreatedAtUtc, UpdatedAtUtc, PayloadReference
            ) VALUES (
                @LocalJobId, @ServerJobId, @WorkType, @State, @AttemptCount, @MaxAttempts, @AvailableAtUtc, @CreatedAtUtc, @UpdatedAtUtc, @PayloadReference
            );
        ";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@LocalJobId", item.LocalJobId);
        cmd.Parameters.AddWithValue("@ServerJobId", item.ServerJobId.ToString());
        cmd.Parameters.AddWithValue("@WorkType", item.WorkType);
        cmd.Parameters.AddWithValue("@State", item.State);
        cmd.Parameters.AddWithValue("@AttemptCount", item.AttemptCount);
        cmd.Parameters.AddWithValue("@MaxAttempts", item.MaxAttempts);
        cmd.Parameters.AddWithValue("@AvailableAtUtc", item.AvailableAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@CreatedAtUtc", item.CreatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@UpdatedAtUtc", item.UpdatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@PayloadReference", item.PayloadReference);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Enqueued local job {LocalJobId} for server job {ServerJobId}", item.LocalJobId, item.ServerJobId);
        return item;
    }

    public async Task<LocalJobItem?> AcquireNextLeaseAsync(string leaseOwner, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);
        using var tx = conn.BeginTransaction();

        var selectSql = @"
            SELECT LocalJobId, ServerJobId, WorkType, State, AttemptCount, MaxAttempts, AvailableAtUtc, LeaseOwner, LeaseExpiresAtUtc, CreatedAtUtc, UpdatedAtUtc, LastErrorCode, PayloadReference
            FROM AgentLocalQueue
            WHERE State = 'Pending' AND AvailableAtUtc <= @Now
            ORDER BY CreatedAtUtc ASC
            LIMIT 1;
        ";

        using var selectCmd = conn.CreateCommand();
        selectCmd.Transaction = tx;
        selectCmd.CommandText = selectSql;
        selectCmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("O"));

        using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var item = MapReaderToJobItem(reader);
        reader.Close();

        var leaseExpires = DateTime.UtcNow.Add(leaseDuration);
        var updateSql = @"
            UPDATE AgentLocalQueue
            SET State = 'Leased',
                LeaseOwner = @LeaseOwner,
                LeaseExpiresAtUtc = @LeaseExpiresAtUtc,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE LocalJobId = @LocalJobId AND State = 'Pending';
        ";

        using var updateCmd = conn.CreateCommand();
        updateCmd.Transaction = tx;
        updateCmd.CommandText = updateSql;
        updateCmd.Parameters.AddWithValue("@LeaseOwner", leaseOwner);
        updateCmd.Parameters.AddWithValue("@LeaseExpiresAtUtc", leaseExpires.ToString("O"));
        updateCmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("O"));
        updateCmd.Parameters.AddWithValue("@LocalJobId", item.LocalJobId);

        var rowsAffected = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        if (rowsAffected > 0)
        {
            item.State = "Leased";
            item.LeaseOwner = leaseOwner;
            item.LeaseExpiresAtUtc = leaseExpires;
            return item;
        }

        return null;
    }

    public async Task CompleteJobAsync(string localJobId, CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE AgentLocalQueue
            SET State = 'Completed',
                LeaseOwner = NULL,
                LeaseExpiresAtUtc = NULL,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE LocalJobId = @LocalJobId;
        ";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@LocalJobId", localJobId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Completed local job {LocalJobId}", localJobId);
    }

    public async Task FailJobAsync(string localJobId, string errorCode, TimeSpan backoff, CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var selectSql = "SELECT AttemptCount, MaxAttempts FROM AgentLocalQueue WHERE LocalJobId = @LocalJobId;";
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = selectSql;
        selectCmd.Parameters.AddWithValue("@LocalJobId", localJobId);

        int attempts = 0, maxAttempts = 5;
        using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                attempts = reader.GetInt32(0);
                maxAttempts = reader.GetInt32(1);
            }
        }

        attempts++;
        var newState = attempts >= maxAttempts ? "Poisoned" : "Pending";
        var availableAt = DateTime.UtcNow.Add(backoff);

        var updateSql = @"
            UPDATE AgentLocalQueue
            SET State = @State,
                AttemptCount = @AttemptCount,
                AvailableAtUtc = @AvailableAtUtc,
                LeaseOwner = NULL,
                LeaseExpiresAtUtc = NULL,
                LastErrorCode = @LastErrorCode,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE LocalJobId = @LocalJobId;
        ";

        using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = updateSql;
        updateCmd.Parameters.AddWithValue("@State", newState);
        updateCmd.Parameters.AddWithValue("@AttemptCount", attempts);
        updateCmd.Parameters.AddWithValue("@AvailableAtUtc", availableAt.ToString("O"));
        updateCmd.Parameters.AddWithValue("@LastErrorCode", errorCode);
        updateCmd.Parameters.AddWithValue("@UpdatedAtUtc", DateTime.UtcNow.ToString("O"));
        updateCmd.Parameters.AddWithValue("@LocalJobId", localJobId);

        await updateCmd.ExecuteNonQueryAsync(cancellationToken);

        if (newState == "Poisoned")
        {
            _logger.LogError("Job {LocalJobId} reached max attempt count ({MaxAttempts}) and is now POISONED.", localJobId, maxAttempts);
        }
        else
        {
            _logger.LogWarning("Job {LocalJobId} failed with code {ErrorCode}. Attempt {AttemptCount}/{MaxAttempts}. Retry available at {AvailableAtUtc}",
                localJobId, errorCode, attempts, maxAttempts, availableAt);
        }
    }

    public async Task RecoverStaleLeasesAsync(CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE AgentLocalQueue
            SET State = 'Pending',
                LeaseOwner = NULL,
                LeaseExpiresAtUtc = NULL,
                UpdatedAtUtc = @Now
            WHERE State = 'Leased' AND LeaseExpiresAtUtc IS NOT NULL AND LeaseExpiresAtUtc < @Now;
        ";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("O"));

        var recovered = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (recovered > 0)
        {
            _logger.LogInformation("Recovered {Count} stale leased jobs back to Pending state.", recovered);
        }
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = "SELECT COUNT(*) FROM AgentLocalQueue WHERE State IN ('Pending', 'Leased');";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
        return count;
    }

    private static LocalJobItem MapReaderToJobItem(SqliteDataReader reader)
    {
        return new LocalJobItem
        {
            LocalJobId = reader.GetString(0),
            ServerJobId = Guid.Parse(reader.GetString(1)),
            WorkType = reader.GetString(2),
            State = reader.GetString(3),
            AttemptCount = reader.GetInt32(4),
            MaxAttempts = reader.GetInt32(5),
            AvailableAtUtc = DateTime.Parse(reader.GetString(6)),
            LeaseOwner = reader.IsDBNull(7) ? null : reader.GetString(7),
            LeaseExpiresAtUtc = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
            CreatedAtUtc = DateTime.Parse(reader.GetString(9)),
            UpdatedAtUtc = DateTime.Parse(reader.GetString(10)),
            LastErrorCode = reader.IsDBNull(11) ? null : reader.GetString(11),
            PayloadReference = reader.GetString(12)
        };
    }
}
