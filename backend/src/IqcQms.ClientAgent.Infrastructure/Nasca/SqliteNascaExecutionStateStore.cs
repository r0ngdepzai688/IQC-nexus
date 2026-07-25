using System.Data;
using IqcQms.ClientAgent.Application.Nasca;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Nasca;

public class SqliteNascaExecutionStateStore : INascaExecutionStateStore
{
    private readonly string _dbPath;
    private readonly ILogger<SqliteNascaExecutionStateStore> _logger;

    public SqliteNascaExecutionStateStore(string rootDataDirectory, ILogger<SqliteNascaExecutionStateStore> logger)
    {
        _logger = logger;
        Directory.CreateDirectory(rootDataDirectory);
        _dbPath = Path.Combine(rootDataDirectory, "nasca_state.db");
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS NascaExecutionStates (
                CorrelationId TEXT PRIMARY KEY,
                QueueItemId TEXT NOT NULL,
                ExecutionId TEXT NOT NULL,
                AttemptNumber INTEGER NOT NULL,
                CurrentState INTEGER NOT NULL,
                SchemaVersion INTEGER NOT NULL,
                WorkDirectoryId TEXT NOT NULL,
                SanitizedReasonCode TEXT,
                CreatedAtUtc TEXT NOT NULL,
                LastUpdatedAtUtc TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection GetConnection()
    {
        return new SqliteConnection($"Data Source={_dbPath};Cache=Shared;");
    }

    public async Task<NascaExecutionStateRecord> CreateInitialStateAsync(string correlationId, string queueItemId, string workDirectoryId, CancellationToken cancellationToken = default)
    {
        var record = new NascaExecutionStateRecord
        {
            CorrelationId = correlationId,
            QueueItemId = queueItemId,
            ExecutionId = Guid.NewGuid().ToString("N"),
            AttemptNumber = 1,
            CurrentState = NascaExecutionState.Queued,
            SchemaVersion = 1,
            WorkDirectoryId = workDirectoryId,
            SanitizedReasonCode = "INITIAL_STATE_CREATED",
            CreatedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        using var conn = GetConnection();
        await conn.OpenAsync(cancellationToken);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO NascaExecutionStates 
            (CorrelationId, QueueItemId, ExecutionId, AttemptNumber, CurrentState, SchemaVersion, WorkDirectoryId, SanitizedReasonCode, CreatedAtUtc, LastUpdatedAtUtc)
            VALUES (@CorrelationId, @QueueItemId, @ExecutionId, @AttemptNumber, @CurrentState, @SchemaVersion, @WorkDirectoryId, @SanitizedReasonCode, @CreatedAtUtc, @LastUpdatedAtUtc);
            """;
        cmd.Parameters.AddWithValue("@CorrelationId", record.CorrelationId);
        cmd.Parameters.AddWithValue("@QueueItemId", record.QueueItemId);
        cmd.Parameters.AddWithValue("@ExecutionId", record.ExecutionId);
        cmd.Parameters.AddWithValue("@AttemptNumber", record.AttemptNumber);
        cmd.Parameters.AddWithValue("@CurrentState", (int)record.CurrentState);
        cmd.Parameters.AddWithValue("@SchemaVersion", record.SchemaVersion);
        cmd.Parameters.AddWithValue("@WorkDirectoryId", record.WorkDirectoryId);
        cmd.Parameters.AddWithValue("@SanitizedReasonCode", (object?)record.SanitizedReasonCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", record.CreatedAtUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@LastUpdatedAtUtc", record.LastUpdatedAtUtc.ToString("o"));

        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Created initial execution state for CorrelationId {CorrelationId}", correlationId);
            return record;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint failed
        {
            _logger.LogWarning("Execution state already exists for CorrelationId {CorrelationId}. Returning existing.", correlationId);
            var existing = await GetStateAsync(correlationId, cancellationToken);
            return existing ?? record;
        }
    }

    public async Task<NascaExecutionStateRecord?> GetStateAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        using var conn = GetConnection();
        await conn.OpenAsync(cancellationToken);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CorrelationId, QueueItemId, ExecutionId, AttemptNumber, CurrentState, SchemaVersion, WorkDirectoryId, SanitizedReasonCode, CreatedAtUtc, LastUpdatedAtUtc FROM NascaExecutionStates WHERE CorrelationId = @CorrelationId;";
        cmd.Parameters.AddWithValue("@CorrelationId", correlationId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return MapRecord(reader);
    }

    public async Task<NascaExecutionStateRecord> TransitionStateAsync(string correlationId, NascaExecutionState expectedFrom, NascaExecutionState targetTo, string? sanitizedReasonCode = null, CancellationToken cancellationToken = default)
    {
        NascaExecutionStateValidator.EnsureValidTransition(expectedFrom, targetTo);

        using var conn = GetConnection();
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);
        using var cmd = conn.CreateCommand();
        cmd.Transaction = (SqliteTransaction)tx;

        var nowStr = DateTime.UtcNow.ToString("o");
        cmd.CommandText = """
            UPDATE NascaExecutionStates 
            SET CurrentState = @TargetTo, SanitizedReasonCode = @ReasonCode, LastUpdatedAtUtc = @Now
            WHERE CorrelationId = @CorrelationId AND CurrentState = @ExpectedFrom;
            """;
        cmd.Parameters.AddWithValue("@TargetTo", (int)targetTo);
        cmd.Parameters.AddWithValue("@ReasonCode", (object?)sanitizedReasonCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Now", nowStr);
        cmd.Parameters.AddWithValue("@CorrelationId", correlationId);
        cmd.Parameters.AddWithValue("@ExpectedFrom", (int)expectedFrom);

        int rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new InvalidOperationException($"Atomic transition failed for CorrelationId {correlationId}: Expected state '{expectedFrom}' did not match current state or state record missing.");
        }

        await tx.CommitAsync(cancellationToken);
        _logger.LogInformation("Transitioned state for CorrelationId {CorrelationId}: {From} -> {To}", correlationId, expectedFrom, targetTo);

        var updated = await GetStateAsync(correlationId, cancellationToken);
        return updated ?? throw new InvalidOperationException($"Failed to reload updated state for {correlationId}.");
    }

    public async Task<NascaExecutionStateRecord> RecordNewAttemptAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var existing = await GetStateAsync(correlationId, cancellationToken);
        if (existing == null)
        {
            throw new InvalidOperationException($"Cannot record new attempt: State record not found for CorrelationId {correlationId}.");
        }

        var newExecutionId = Guid.NewGuid().ToString("N");
        var nowStr = DateTime.UtcNow.ToString("o");

        using var conn = GetConnection();
        await conn.OpenAsync(cancellationToken);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE NascaExecutionStates 
            SET AttemptNumber = AttemptNumber + 1, ExecutionId = @ExecutionId, CurrentState = @TargetState, LastUpdatedAtUtc = @Now
            WHERE CorrelationId = @CorrelationId;
            """;
        cmd.Parameters.AddWithValue("@ExecutionId", newExecutionId);
        cmd.Parameters.AddWithValue("@TargetState", (int)NascaExecutionState.ExecutionPending);
        cmd.Parameters.AddWithValue("@Now", nowStr);
        cmd.Parameters.AddWithValue("@CorrelationId", correlationId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Incremented attempt for CorrelationId {CorrelationId}. New attempt executionId: {ExecutionId}", correlationId, newExecutionId);

        var updated = await GetStateAsync(correlationId, cancellationToken);
        return updated ?? throw new InvalidOperationException($"Failed to reload updated state for {correlationId}.");
    }

    public async Task<IReadOnlyList<NascaExecutionStateRecord>> GetRecoverableExecutionsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<NascaExecutionStateRecord>();
        using var conn = GetConnection();
        await conn.OpenAsync(cancellationToken);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT CorrelationId, QueueItemId, ExecutionId, AttemptNumber, CurrentState, SchemaVersion, WorkDirectoryId, SanitizedReasonCode, CreatedAtUtc, LastUpdatedAtUtc 
            FROM NascaExecutionStates 
            WHERE CurrentState NOT IN (@CompletedState, @CancelledState);
            """;
        cmd.Parameters.AddWithValue("@CompletedState", (int)NascaExecutionState.Completed);
        cmd.Parameters.AddWithValue("@CancelledState", (int)NascaExecutionState.Cancelled);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapRecord(reader));
        }

        return results;
    }

    public async Task<NascaExecutionStateRecord> MarkRecoveryRequiredAsync(string correlationId, string sanitizedReasonCode, CancellationToken cancellationToken = default)
    {
        using var conn = GetConnection();
        await conn.OpenAsync(cancellationToken);
        using var cmd = conn.CreateCommand();
        var nowStr = DateTime.UtcNow.ToString("o");
        cmd.CommandText = """
            UPDATE NascaExecutionStates 
            SET CurrentState = @RecoveryState, SanitizedReasonCode = @ReasonCode, LastUpdatedAtUtc = @Now
            WHERE CorrelationId = @CorrelationId;
            """;
        cmd.Parameters.AddWithValue("@RecoveryState", (int)NascaExecutionState.RecoveryRequired);
        cmd.Parameters.AddWithValue("@ReasonCode", sanitizedReasonCode);
        cmd.Parameters.AddWithValue("@Now", nowStr);
        cmd.Parameters.AddWithValue("@CorrelationId", correlationId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogWarning("Marked CorrelationId {CorrelationId} as RecoveryRequired: {ReasonCode}", correlationId, sanitizedReasonCode);

        var updated = await GetStateAsync(correlationId, cancellationToken);
        return updated ?? throw new InvalidOperationException($"Failed to reload state for {correlationId}.");
    }

    private static NascaExecutionStateRecord MapRecord(SqliteDataReader reader)
    {
        var rawState = reader.GetInt32(4);
        if (!Enum.IsDefined(typeof(NascaExecutionState), rawState))
        {
            throw new InvalidOperationException($"Corrupt state value '{rawState}' in SQLite database.");
        }

        return new NascaExecutionStateRecord
        {
            CorrelationId = reader.GetString(0),
            QueueItemId = reader.GetString(1),
            ExecutionId = reader.GetString(2),
            AttemptNumber = reader.GetInt32(3),
            CurrentState = (NascaExecutionState)rawState,
            SchemaVersion = reader.GetInt32(5),
            WorkDirectoryId = reader.GetString(6),
            SanitizedReasonCode = reader.IsDBNull(7) ? null : reader.GetString(7),
            CreatedAtUtc = DateTime.Parse(reader.GetString(8)),
            LastUpdatedAtUtc = DateTime.Parse(reader.GetString(9))
        };
    }
}
