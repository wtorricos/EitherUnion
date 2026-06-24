namespace WTorricos.Either;

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
    public Failure(
        string ErrorCode,
        string Message,
        Severity Level,
        DateTimeOffset Timestamp,
        IReadOnlyList<Detail> Details,
        string? TraceId = null,
        string? StackTrace = null,
        Failure? InnerError = null,
        IReadOnlyDictionary<string, object>? Metadata = null)
        : this(
            ErrorCode,
            Message,
            Level,
            Timestamp.UtcDateTime,
            Details,
            TraceId,
            StackTrace,
            InnerError,
            Metadata)
    {
    }

    public IReadOnlyDictionary<string, object>? Metadata { get; init; } =
        Metadata is null
            ? null
            : new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(new Dictionary<string, object>(Metadata));

    public DateTimeOffset TimestampOffset =>
        new DateTimeOffset(DateTime.SpecifyKind(Timestamp, DateTimeKind.Utc));

    public string GetDisplayMessage()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(Message);

        if (Details.Count > 0)
        {
            foreach (Detail detail in Details)
            {
                sb.Append(System.Environment.NewLine);
                sb.Append(detail.Code).Append(": ").Append(detail.Description);
            }
        }

        if (InnerError is not null)
        {
            sb.Append(System.Environment.NewLine);
            sb.Append("Inner Error: ").Append(InnerError.GetDisplayMessage());
        }

        return sb.ToString();
    }
}
