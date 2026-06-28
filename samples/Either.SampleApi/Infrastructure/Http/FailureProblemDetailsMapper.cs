using System.Globalization;
using WTorricos.Either;

namespace Either.SampleApi.Infrastructure.Http;

public static class FailureProblemDetailsMapper
{
    public static IResult ToProblemResult(this Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        int statusCode = MapStatusCode(failure);
        Dictionary<string, object?> extensions = new()
        {
            ["errorCode"] = failure.ErrorCode,
            ["severity"] = failure.Level.ToString(),
            ["timestamp"] = failure.TimestampOffset.ToString("O"),
            ["details"] = failure.Details.Select(detail => new { detail.Code, detail.Description }).ToArray()
        };

        if (failure.TraceId is not null)
        {
            extensions["traceId"] = failure.TraceId;
        }

        return Results.Problem(
            statusCode: statusCode,
            title: failure.ErrorCode,
            detail: failure.Message,
            extensions: extensions);
    }

    private static int MapStatusCode(Failure failure) =>
        int.TryParse(failure.ErrorCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericStatusCode) && numericStatusCode is >= StatusCodes.Status100Continue and <= 599
            ? numericStatusCode
            : failure.ErrorCode.StartsWith("VALIDATION_", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status400BadRequest
            : failure.ErrorCode.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status404NotFound
            : failure.ErrorCode.StartsWith("CONFLICT_", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status409Conflict
            : failure.Level switch
            {
                Severity.Info => StatusCodes.Status400BadRequest,
                Severity.Warning => StatusCodes.Status400BadRequest,
                Severity.Error => StatusCodes.Status500InternalServerError,
                Severity.Critical => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            };
}
