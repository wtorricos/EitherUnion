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
    Dictionary<string, object>? Metadata = null)
{
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
