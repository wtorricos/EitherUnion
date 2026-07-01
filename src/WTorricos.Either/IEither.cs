namespace WTorricos.Either;

/// <summary>
/// IEither represents a discriminated union of two types: Failure (error) or Ok (success value).
/// This is v1's core type using C# union types for expressive, type-safe error handling.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly union IEither<T>(Failure, Ok<T>);

public record Ok<T>(T Value);

public record Failure(
    string ErrorCode,
    string Message,
    Severity Level,
    DateTime Timestamp,
    IReadOnlyList<Detail> Details,
    string? TraceId = null,
    string? StackTrace = null,
    Failure? InnerError = null,
    IReadOnlyDictionary<string, object>? Metadata = null)
{
    public static Failure NullFailure(string parameterName) =>
        new(
            ErrorCode: "NULL_FAILURE",
            Message: $"Required parameter '{parameterName}' was null.",
            Level: Severity.Error,
            Timestamp: DateTime.UtcNow,
            Details: [new Detail("NULL_PARAMETER", parameterName)]);

    public Failure(
        string errorCode,
        string message,
        Severity level,
        DateTimeOffset timestamp,
        IReadOnlyList<Detail> details,
        string? traceId = null,
        string? stackTrace = null,
        Failure? innerError = null,
        IReadOnlyDictionary<string, object>? metadata = null)
        : this(
            errorCode,
            message,
            level,
            timestamp.UtcDateTime,
            details,
            traceId,
            stackTrace,
            innerError,
            metadata)
    {
    }

    public IReadOnlyDictionary<string, object>? Metadata { get; init; } =
        Metadata is null
            ? null
            : new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new Dictionary<string, object>(Metadata));

    public DateTimeOffset TimestampOffset =>
        new(DateTime.SpecifyKind(Timestamp, DateTimeKind.Utc));

    public string GetDisplayMessage()
    {
        System.Text.StringBuilder sb = new(Message);

        if (Details.Count > 0)
        {
            foreach (Detail detail in Details)
            {
                _ = sb.Append(Environment.NewLine);
                _ = sb.Append(detail.Code).Append(": ").Append(detail.Description);
            }
        }

        if (InnerError is not null)
        {
            _ = sb.Append(Environment.NewLine);
            _ = sb.Append("Inner Error: ").Append(InnerError.GetDisplayMessage());
        }

        return sb.ToString();
    }
}

public record Detail(string Code, string Description);

public enum Severity
{
    Info,
    Warning,
    Error,
    Critical
}
