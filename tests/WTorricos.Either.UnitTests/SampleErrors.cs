namespace WTorricos.Either.UnitTests;

public record DivideByZeroError(
    string Message = "Division by zero is not allowed",
    string? TraceId = null)
    : Failure(
        ErrorCode: "DIVIDE_BY_ZERO",
        Message,
        Level: Severity.Error,
        Timestamp: DateTime.UtcNow,
        Details: [],
        TraceId: TraceId,
        StackTrace: Environment.StackTrace);

public record ValidationError(
    string Message = "Validation failed",
    IReadOnlyList<Detail>? Details = null,
    string? TraceId = null)
    : Failure(
        ErrorCode: "VALIDATION_FAILED",
        Message,
        Level: Severity.Warning,
        Timestamp: DateTime.UtcNow,
        Details: Details ?? [],
        TraceId: TraceId);
