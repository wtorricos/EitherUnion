using Either.SampleApi.Infrastructure.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WTorricos.Either;

namespace Either.SampleApi.Tests;

public sealed class FailureProblemDetailsMapperTest
{
    [Fact(DisplayName = "ToProblemResult includes traceId when it is provided")]
    public void ToProblemResultIncludesTraceIdWhenProvided()
    {
        Failure failure = new(
            ErrorCode: "DOMAIN_FAILURE",
            Message: "A domain error occurred.",
            Level: Severity.Info,
            Timestamp: DateTime.UtcNow,
            Details: [new Detail("RULE", "A rule was violated.")],
            TraceId: "trace-123");

        IResult result = failure.ToProblemResult();
        IStatusCodeHttpResult statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        IValueHttpResult valueResult = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        ProblemDetails payload = Assert.IsType<ProblemDetails>(valueResult.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
        Assert.Equal("trace-123", payload.Extensions["traceId"]);
    }

    [Theory(DisplayName = "ToProblemResult maps non-pattern failures from severity")]
    [InlineData(Severity.Info, StatusCodes.Status400BadRequest)]
    [InlineData(Severity.Warning, StatusCodes.Status400BadRequest)]
    [InlineData(Severity.Error, StatusCodes.Status500InternalServerError)]
    [InlineData(Severity.Critical, StatusCodes.Status500InternalServerError)]
    [InlineData((Severity)999, StatusCodes.Status500InternalServerError)]
    public void ToProblemResultMapsSeverityWhenErrorCodeDoesNotMatchKnownPatterns(Severity severity, int expectedStatusCode)
    {
        Failure failure = new(
            ErrorCode: "DOMAIN_FAILURE",
            Message: "A domain error occurred.",
            Level: severity,
            Timestamp: DateTime.UtcNow,
            Details: [new Detail("RULE", "A rule was violated.")]);

        IResult result = failure.ToProblemResult();
        IStatusCodeHttpResult statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        IValueHttpResult valueResult = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        ProblemDetails payload = Assert.IsType<ProblemDetails>(valueResult.Value);

        Assert.Equal(expectedStatusCode, statusCodeResult.StatusCode);
        Assert.Equal("DOMAIN_FAILURE", payload.Extensions["errorCode"]);
    }
}
