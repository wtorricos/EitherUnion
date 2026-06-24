namespace WTorricos.Either;

public static class IEitherQueryExtensions
{
    public static IEither<TResult> Select<T, TResult>(this IEither<T> either, Func<T, TResult> selector) =>
        either.Map(selector);

    public static IEither<TResult> SelectMany<T, TResult>(this IEither<T> either, Func<T, IEither<TResult>> selector) =>
        either.FlatMap(selector);

    public static IEither<TResult> SelectMany<T, TCollection, TResult>(
        this IEither<T> either,
        Func<T, IEither<TCollection>> collectionSelector,
        Func<T, TCollection, TResult> resultSelector) => either.FlatMap(value =>
                                                                  collectionSelector(value).Map(collection => resultSelector(value, collection)));

    public static IEither<T> Where<T>(
        this IEither<T> either,
        Func<T, bool> predicate,
        Failure? failure = null)
    {
        Failure predicateFailure = failure ?? new Failure(
            ErrorCode: "PREDICATE_FAILED",
            Message: "The LINQ predicate returned false.",
            Level: Severity.Warning,
            Timestamp: DateTime.UtcNow,
            Details: []);

        return either.Filter(predicate, predicateFailure);
    }
}
