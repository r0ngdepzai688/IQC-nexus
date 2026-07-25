using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IqcQms.Infrastructure.Security;

public class RelationalConstraintViolationClassifier : IRelationalConstraintViolationClassifier
{
    public RelationalConstraintViolationType Classify(DbUpdateException exception)
    {
        if (exception == null)
            return RelationalConstraintViolationType.None;

        var current = exception.InnerException ?? exception;

        // Traverse exception chain
        while (current != null)
        {
            if (current is SqliteException sqliteEx)
            {
                return ClassifySqliteException(sqliteEx);
            }

            // Fallback string matching for relational providers (SQLite, SqlClient, Postgres, etc.)
            var msg = current.Message ?? string.Empty;
            if (msg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("PRIMARY KEY constraint", StringComparison.OrdinalIgnoreCase))
            {
                return RelationalConstraintViolationType.UniqueConstraintViolation;
            }

            if (msg.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("foreign key constraint", StringComparison.OrdinalIgnoreCase))
            {
                return RelationalConstraintViolationType.ForeignKeyViolation;
            }

            if (msg.Contains("NOT NULL constraint failed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("cannot insert NULL", StringComparison.OrdinalIgnoreCase))
            {
                return RelationalConstraintViolationType.NotNullViolation;
            }

            if (msg.Contains("transaction", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
            {
                return RelationalConstraintViolationType.TransactionFailure;
            }

            if (msg.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("network", StringComparison.OrdinalIgnoreCase))
            {
                return RelationalConstraintViolationType.ConnectionFailure;
            }

            current = current.InnerException;
        }

        return RelationalConstraintViolationType.UnknownDatabaseError;
    }

    public bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return Classify(exception) == RelationalConstraintViolationType.UniqueConstraintViolation;
    }

    private static RelationalConstraintViolationType ClassifySqliteException(SqliteException sqliteEx)
    {
        // Check SQLite extended error code first
        switch (sqliteEx.SqliteExtendedErrorCode)
        {
            case 2067: // SQLITE_CONSTRAINT_UNIQUE
            case 1555: // SQLITE_CONSTRAINT_PRIMARYKEY
                return RelationalConstraintViolationType.UniqueConstraintViolation;
            case 787:  // SQLITE_CONSTRAINT_FOREIGNKEY
                return RelationalConstraintViolationType.ForeignKeyViolation;
            case 1299: // SQLITE_CONSTRAINT_NOTNULL
                return RelationalConstraintViolationType.NotNullViolation;
        }

        // Check SQLite base error code & message
        if (sqliteEx.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
        {
            var msg = sqliteEx.Message;
            if (msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) || msg.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                return RelationalConstraintViolationType.UniqueConstraintViolation;
            if (msg.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                return RelationalConstraintViolationType.ForeignKeyViolation;
            if (msg.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase))
                return RelationalConstraintViolationType.NotNullViolation;
        }

        if (sqliteEx.SqliteErrorCode == 5 || sqliteEx.SqliteErrorCode == 6) // SQLITE_BUSY or SQLITE_LOCKED
        {
            return RelationalConstraintViolationType.TransactionFailure;
        }

        return RelationalConstraintViolationType.UnknownDatabaseError;
    }
}
