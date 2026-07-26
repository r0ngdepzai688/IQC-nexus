namespace IqcQms.Domain.Exceptions;

public abstract class PayloadSubmissionException : Exception
{
    public string ReasonCode { get; }

    protected PayloadSubmissionException(string message, string reasonCode)
        : base(message)
    {
        ReasonCode = reasonCode;
    }

    protected PayloadSubmissionException(string message, string reasonCode, Exception innerException)
        : base(message, innerException)
    {
        ReasonCode = reasonCode;
    }
}

public abstract class PayloadSubmissionSecurityException : PayloadSubmissionException
{
    protected PayloadSubmissionSecurityException(string message, string reasonCode)
        : base(message, reasonCode)
    {
    }

    protected PayloadSubmissionSecurityException(string message, string reasonCode, Exception innerException)
        : base(message, reasonCode, innerException)
    {
    }
}

public class PayloadSubmissionMismatchException : PayloadSubmissionSecurityException
{
    public PayloadSubmissionMismatchException(string message = "Payload submission identity or digest mismatch.", string reasonCode = "SUBMISSION_MISMATCH")
        : base(message, reasonCode)
    {
    }
}

public class PayloadNonceReplayException : PayloadSubmissionSecurityException
{
    public PayloadNonceReplayException(string message = "Payload nonce replay detected.", string reasonCode = "NONCE_REPLAY")
        : base(message, reasonCode)
    {
    }
}

public class PayloadReplayTombstoneException : PayloadSubmissionSecurityException
{
    public PayloadReplayTombstoneException(string message = "Payload submission has expired and is archived as a tombstone.", string reasonCode = "TOMBSTONE_HIT")
        : base(message, reasonCode)
    {
    }
}

public class PayloadSubmissionValidationException : PayloadSubmissionException
{
    public PayloadSubmissionValidationException(string message = "Payload submission validation failed.", string reasonCode = "SUBMISSION_VALIDATION_FAILED")
        : base(message, reasonCode)
    {
    }
}

public class PayloadSubmissionOwnershipException : PayloadSubmissionException
{
    public PayloadSubmissionOwnershipException(string message = "Device or payload submission ownership conflict.", string reasonCode = "SUBMISSION_OWNERSHIP_CONFLICT")
        : base(message, reasonCode)
    {
    }
}
