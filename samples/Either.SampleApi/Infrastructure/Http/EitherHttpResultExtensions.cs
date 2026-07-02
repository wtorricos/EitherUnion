using WTorricos.Either;

namespace Either.SampleApi.Infrastructure.Http;

/// <summary>
/// Sample helper to Map IEither<T> to IResult
/// </summary>
public static class EitherHttpResultExtensions
{
    public static IResult ToOkResult<T>(this IEither<T> either) =>
        either.Match(
            onSuccess: value => Results.Ok(value),
            onFailure: failure => failure.ToProblemResult());

    public static IResult ToCreatedResult<T>(this IEither<T> either, Func<T, string> locationFactory) =>
        locationFactory is null
            ? throw new ArgumentNullException(nameof(locationFactory))
            : either.Match(
            onSuccess: value => Results.Created(locationFactory(value), value),
            onFailure: failure => failure.ToProblemResult());

    public static IResult ToAcceptedResult<T>(this IEither<T> either) =>
        either.Match(
            onSuccess: _ => Results.StatusCode(StatusCodes.Status202Accepted),
            onFailure: failure => failure.ToProblemResult());

    public static IResult ToNoContentResult<T>(this IEither<T> either) =>
        either.Match(
            onSuccess: _ => Results.NoContent(),
            onFailure: failure => failure.ToProblemResult());
}
