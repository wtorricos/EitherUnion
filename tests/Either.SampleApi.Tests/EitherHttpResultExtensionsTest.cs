using Either.SampleApi.Infrastructure.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WTorricos.Either;

namespace Either.SampleApi.Tests;

public sealed class EitherHttpResultExtensionsTest
{
    [Fact(DisplayName = "ToOkResult returns OK for successful values")]
    public void ToOkResultReturnsOkForSuccessfulValues()
    {
        IEither<string> result = new Ok<string>("sample");

        IResult httpResult = result.ToOkResult();
        IStatusCodeHttpResult statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(httpResult, exactMatch: false);
        IValueHttpResult valueResult = Assert.IsType<IValueHttpResult>(httpResult, exactMatch: false);

        Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);
        Assert.Equal("sample", valueResult.Value);
    }

    [Fact(DisplayName = "ToCreatedResult returns Created for successful values")]
    public void ToCreatedResultReturnsCreatedForSuccessfulValues()
    {
        IEither<string> result = new Ok<string>("sample");

        IResult httpResult = result.ToCreatedResult(value => $"/samples/{value}");
        IStatusCodeHttpResult statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(httpResult, exactMatch: false);
        IValueHttpResult valueResult = Assert.IsType<IValueHttpResult>(httpResult, exactMatch: false);

        Assert.Equal(StatusCodes.Status201Created, statusCodeResult.StatusCode);
        Assert.Equal("sample", valueResult.Value);
    }

    [Fact(DisplayName = "ToAcceptedResult returns Accepted for successful values")]
    public void ToAcceptedResultReturnsAcceptedForSuccessfulValues()
    {
        IEither<string> result = new Ok<string>("sample");

        IResult httpResult = result.ToAcceptedResult();
        IStatusCodeHttpResult statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(httpResult, exactMatch: false);

        Assert.Equal(StatusCodes.Status202Accepted, statusCodeResult.StatusCode);
    }

    [Fact(DisplayName = "ToNoContentResult returns NoContent for successful values")]
    public void ToNoContentResultReturnsNoContentForSuccessfulValues()
    {
        IEither<string> result = new Ok<string>("sample");

        IResult httpResult = result.ToNoContentResult();
        IStatusCodeHttpResult statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(httpResult, exactMatch: false);

        Assert.Equal(StatusCodes.Status204NoContent, statusCodeResult.StatusCode);
    }

    [Fact(DisplayName = "ToProblemResult uses numeric error codes as HTTP status codes")]
    public void ToProblemResultUsesNumericErrorCodesAsHttpStatusCodes()
    {
        Failure failure = new(
            ErrorCode: "418",
            Message: "I'm a teapot.",
            Level: Severity.Warning,
            Timestamp: DateTime.UtcNow,
            Details: []);

        IResult result = failure.ToProblemResult();
        IStatusCodeHttpResult statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        IValueHttpResult valueResult = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        ProblemDetails payload = Assert.IsType<ProblemDetails>(valueResult.Value);

        Assert.Equal(418, statusCodeResult.StatusCode);
        Assert.Equal(418, payload.Status);
        Assert.Equal("418", payload.Title);
    }
}
