namespace WTorricos.Either;

public static class IEitherAsyncExtensions
{
    public static async ValueTask<IEither<TOut>> MapAsync<TIn, TOut>(
        this IEither<TIn> either,
        Func<TIn, ValueTask<TOut>> map,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(map);
        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<TIn> ok => new Ok<TOut>(await map(ok.Value).ConfigureAwait(false)),
            Failure failure => failure
        };
    }

    public static async ValueTask<IEither<TOut>> FlatMapAsync<TIn, TOut>(
        this IEither<TIn> either,
        Func<TIn, ValueTask<IEither<TOut>>> bind,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bind);
        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<TIn> ok => await bind(ok.Value).ConfigureAwait(false),
            Failure failure => failure
        };
    }

    public static async ValueTask<TResult> MatchAsync<T, TResult>(
        this IEither<T> either,
        Func<T, ValueTask<TResult>> onSuccess,
        Func<Failure, ValueTask<TResult>> onFailure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<T> ok => await onSuccess(ok.Value).ConfigureAwait(false),
            Failure failure => await onFailure(failure).ConfigureAwait(false)
        };
    }

    public static async ValueTask ActionAsync<T>(
        this IEither<T> either,
        Func<T, ValueTask> onSuccess,
        Func<Failure, ValueTask>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        cancellationToken.ThrowIfCancellationRequested();

        if (either is Ok<T> ok)
        {
            await onSuccess(ok.Value).ConfigureAwait(false);
            return;
        }

        if (either is Failure failure && onFailure is not null)
        {
            await onFailure(failure).ConfigureAwait(false);
        }
    }
}
