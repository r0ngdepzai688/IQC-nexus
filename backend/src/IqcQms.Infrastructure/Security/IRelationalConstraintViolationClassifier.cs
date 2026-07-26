using Microsoft.EntityFrameworkCore;

namespace IqcQms.Infrastructure.Security;

public enum RelationalConstraintViolationType
{
    None,
    UniqueConstraintViolation,
    ForeignKeyViolation,
    NotNullViolation,
    TransactionFailure,
    ConnectionFailure,
    UnknownDatabaseError
}

public interface IRelationalConstraintViolationClassifier
{
    RelationalConstraintViolationType Classify(DbUpdateException exception);
    bool IsUniqueConstraintViolation(DbUpdateException exception);
}
