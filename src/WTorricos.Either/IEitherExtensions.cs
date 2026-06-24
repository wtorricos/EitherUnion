namespace WTorricos.Either;

/// <summary>
/// Fluent extension methods for IEither{T} to support monadic composition and error handling.
/// </summary>
public static class IEitherExtensions
{
    /// <summary>
    /// Transforms the success value using the provided mapping function.
    /// If the Either is a Failure, it is returned unchanged.
    /// </summary>
    public static IEither<TOut> Map<TIn, TOut>(this IEither<TIn> either, Func<TIn, TOut> map) => either switch
    {
        Ok<TIn> ok => new Ok<TOut>(map(ok.Value)),
        Failure failure => failure
    };

    /// <summary>
    /// Monadic bind operation. Chains Either-returning operations.
    /// If the Either is a Failure, it is returned unchanged.
    /// </summary>
    public static IEither<TOut> FlatMap<TIn, TOut>(this IEither<TIn> either, Func<TIn, IEither<TOut>> bind) => either switch
    {
        Ok<TIn> ok => bind(ok.Value),
        Failure failure => failure
    };

    /// <summary>
    /// Unwraps a nested Either{Either{T}} into a flat Either{T}.
    /// </summary>
    public static IEither<T> Flatten<T>(this IEither<IEither<T>> either) => either switch
    {
        Ok<IEither<T>> ok => ok.Value,
        Failure failure => failure
    };

    /// <summary>
    /// Transforms a failure using the provided error mapping function.
    /// If the Either is a success, it is returned unchanged.
    /// </summary>
    public static IEither<T> MapFailure<T>(this IEither<T> either, Func<Failure, Failure> mapError) => either switch
    {
        Ok<T> ok => ok,
        Failure failure => mapError(failure)
    };

    /// <summary>
    /// Extracts the success value or throws if the Either is a Failure.
    /// </summary>
    public static T GetValueOrThrow<T>(this IEither<T> either) => either switch
    {
        Ok<T> ok => ok.Value,
        Failure failure => throw new InvalidOperationException(failure.GetDisplayMessage())
    };

    /// <summary>
    /// Returns the Either unchanged (useful for logging or side effects in chains).
    /// </summary>
    public static IEither<T> Inspect<T>(this IEither<T> either, Action<T>? onSuccess = null, Action<Failure>? onFailure = null)
    {
        if (either is Ok<T> ok)
        {
            onSuccess?.Invoke(ok.Value);
        }
        else if (either is Failure failure)
        {
            onFailure?.Invoke(failure);
        }

        return either;
    }

    /// <summary>
    /// Filters the success value using the provided predicate.
    /// If predicate is false, returns a Failure with the provided error.
    /// </summary>
    public static IEither<T> Filter<T>(this IEither<T> either, Func<T, bool> predicate, Failure filterFailure) => either switch
    {
        Ok<T> ok when predicate(ok.Value) => ok,
        Ok<T> _ => filterFailure,
        Failure failure => failure
    };

    /// <summary>
    /// Converts a nullable value into an Either{T}.
    /// Returns Failure if value is null.
    /// </summary>
    public static IEither<T> FromNullable<T>(T? value, Failure whenNull) where T : class => value is not null ? new Ok<T>(value) : whenNull;

    /// <summary>
    /// Converts an Either{T} into an Either{TOut}, discarding the success value.
    /// Useful for operations where you only care about success/failure, not the value.
    /// </summary>
    public static IEither<Unit> Void<T>(this IEither<T> either) => either switch
    {
        Ok<T> _ => new Ok<Unit>(Unit.Instance),
        Failure failure => failure
    };
}
