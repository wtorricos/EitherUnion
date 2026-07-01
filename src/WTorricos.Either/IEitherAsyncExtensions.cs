namespace WTorricos.Either;

public static class IEitherAsyncExtensions
{
    private static Task<IEither<T>> AwaitEitherAsync<T>(
        Task<IEither<T>> eitherTask,
        CancellationToken cancellationToken)
    {
        if (eitherTask is null)
        {
            return Task.FromResult<IEither<T>>(Failure.NullFailure(nameof(eitherTask)));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return eitherTask;
    }

    public static async Task<IEither<TOut>> MapAsync<TIn, TOut>(
        this IEither<TIn> either,
        Func<TIn, Task<TOut>> map,
        CancellationToken cancellationToken = default)
    {
        if (map is null)
        {
            return Failure.NullFailure(nameof(map));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<TIn> ok => new Ok<TOut>(await map(ok.Value).ConfigureAwait(false)),
            Failure failure => failure
        };
    }

    public static async Task<IEither<TOut>> MapAsync<TIn, TOut>(
        this Task<IEither<TIn>> eitherTask,
        Func<TIn, Task<TOut>> map,
        CancellationToken cancellationToken = default)
    {
        if (map is null)
        {
            return Failure.NullFailure(nameof(map));
        }

        IEither<TIn> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.MapAsync(map, cancellationToken).ConfigureAwait(false);
    }

    public static Task<IEither<TOut>> MapAsync<TIn, TOut>(
        this Task<IEither<TIn>> eitherTask,
        Func<TIn, TOut> map,
        CancellationToken cancellationToken = default) =>
        map is null
            ? Task.FromResult<IEither<TOut>>(Failure.NullFailure(nameof(map)))
            : eitherTask.MapAsync(value => Task.FromResult(map(value)), cancellationToken);

    public static async ValueTask<IEither<TOut>> MapAsync<TIn, TOut>(
        this IEither<TIn> either,
        Func<TIn, ValueTask<TOut>> map,
        CancellationToken cancellationToken = default)
    {
        if (map is null)
        {
            return Failure.NullFailure(nameof(map));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<TIn> ok => new Ok<TOut>(await map(ok.Value).ConfigureAwait(false)),
            Failure failure => failure
        };
    }

    public static async Task<IEither<TOut>> FlatMapAsync<TIn, TOut>(
        this IEither<TIn> either,
        Func<TIn, Task<IEither<TOut>>> bind,
        CancellationToken cancellationToken = default)
    {
        if (bind is null)
        {
            return Failure.NullFailure(nameof(bind));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<TIn> ok => await bind(ok.Value).ConfigureAwait(false),
            Failure failure => failure
        };
    }

    public static async Task<IEither<TOut>> FlatMapAsync<TIn, TOut>(
        this Task<IEither<TIn>> eitherTask,
        Func<TIn, Task<IEither<TOut>>> bind,
        CancellationToken cancellationToken = default)
    {
        if (bind is null)
        {
            return Failure.NullFailure(nameof(bind));
        }

        IEither<TIn> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.FlatMapAsync(bind, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<IEither<TOut>> FlatMapAsync<TIn, TOut>(
        this IEither<TIn> either,
        Func<TIn, ValueTask<IEither<TOut>>> bind,
        CancellationToken cancellationToken = default)
    {
        if (bind is null)
        {
            return Failure.NullFailure(nameof(bind));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<TIn> ok => await bind(ok.Value).ConfigureAwait(false),
            Failure failure => failure
        };
    }

    public static async Task<TResult> MatchAsync<T, TResult>(
        this IEither<T> either,
        Func<T, Task<TResult>> onSuccess,
        Func<Failure, Task<TResult>> onFailure,
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

    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<IEither<T>> eitherTask,
        Func<T, Task<TResult>> onSuccess,
        Func<Failure, Task<TResult>> onFailure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        IEither<T> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.MatchAsync(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    public static Task<TResult> MatchAsync<T, TResult>(
        this Task<IEither<T>> eitherTask,
        Func<T, TResult> onSuccess,
        Func<Failure, TResult> onFailure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return eitherTask.MatchAsync(
            onSuccess: value => Task.FromResult(onSuccess(value)),
            onFailure: failure => Task.FromResult(onFailure(failure)),
            cancellationToken: cancellationToken);
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

    public static async Task ActionAsync<T>(
        this IEither<T> either,
        Func<T, Task> onSuccess,
        Func<Failure, Task>? onFailure = null,
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

    public static async Task ActionAsync<T>(
        this Task<IEither<T>> eitherTask,
        Func<T, Task> onSuccess,
        Func<Failure, Task>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        IEither<T> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        await either.ActionAsync(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    public static Task ActionAsync<T>(
        this Task<IEither<T>> eitherTask,
        Action<T> onSuccess,
        Action<Failure>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        return eitherTask.ActionAsync(
            onSuccess: value =>
            {
                onSuccess(value);
                return Task.CompletedTask;
            },
            onFailure: onFailure is null
                ? null
                : failure =>
                {
                    onFailure(failure);
                    return Task.CompletedTask;
                },
            cancellationToken: cancellationToken);
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

    public static async Task<IEither<T>> FlattenAsync<T>(
        this IEither<Task<IEither<T>>> either,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<Task<IEither<T>>> ok => await ok.Value.ConfigureAwait(false),
            Failure failure => failure
        };
    }

    public static async Task<IEither<T>> FlattenAsync<T>(
        this Task<IEither<Task<IEither<T>>>> eitherTask,
        CancellationToken cancellationToken = default)
    {
        IEither<Task<IEither<T>>> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.FlattenAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IEither<T>> MapFailureAsync<T>(
        this IEither<T> either,
        Func<Failure, Task<Failure>> mapError,
        CancellationToken cancellationToken = default)
    {
        if (mapError is null)
        {
            return Failure.NullFailure(nameof(mapError));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<T> ok => ok,
            Failure failure => await mapError(failure).ConfigureAwait(false)
        };
    }

    public static async Task<IEither<T>> MapFailureAsync<T>(
        this Task<IEither<T>> eitherTask,
        Func<Failure, Task<Failure>> mapError,
        CancellationToken cancellationToken = default)
    {
        if (mapError is null)
        {
            return Failure.NullFailure(nameof(mapError));
        }

        IEither<T> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.MapFailureAsync(mapError, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IEither<T>> InspectAsync<T>(
        this IEither<T> either,
        Func<T, Task>? onSuccess = null,
        Func<Failure, Task>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (either is Ok<T> ok && onSuccess is not null)
        {
            await onSuccess(ok.Value).ConfigureAwait(false);
        }
        else if (either is Failure failure && onFailure is not null)
        {
            await onFailure(failure).ConfigureAwait(false);
        }

        return either;
    }

    public static async Task<IEither<T>> InspectAsync<T>(
        this Task<IEither<T>> eitherTask,
        Action<T>? onSuccess = null,
        Action<Failure>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        IEither<T> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.InspectAsync(
            onSuccess: onSuccess is null ? null : value =>
            {
                onSuccess(value);
                return Task.CompletedTask;
            },
            onFailure: onFailure is null ? null : failure =>
            {
                onFailure(failure);
                return Task.CompletedTask;
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IEither<T>> InspectAsync<T>(
        this Task<IEither<T>> eitherTask,
        Func<T, Task>? onSuccess = null,
        Func<Failure, Task>? onFailure = null,
        CancellationToken cancellationToken = default)
    {
        IEither<T> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.InspectAsync(onSuccess, onFailure, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IEither<T>> TapAsync<T>(
        this IEither<T> either,
        Func<T, Task> onSuccess,
        CancellationToken cancellationToken = default)
    {
        if (onSuccess is null)
        {
            return Failure.NullFailure(nameof(onSuccess));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (either is Ok<T> ok)
        {
            await onSuccess(ok.Value).ConfigureAwait(false);
        }

        return either;
    }

    public static Task<IEither<T>> TapAsync<T>(
        this Task<IEither<T>> eitherTask,
        Action<T> onSuccess,
        CancellationToken cancellationToken = default) =>
        onSuccess is null
            ? Task.FromResult<IEither<T>>(Failure.NullFailure(nameof(onSuccess)))
            : eitherTask.TapAsync(
                onSuccess: value =>
                {
                    onSuccess(value);
                    return Task.CompletedTask;
                },
                cancellationToken: cancellationToken);

    public static async Task<IEither<T>> TapAsync<T>(
        this Task<IEither<T>> eitherTask,
        Func<T, Task> onSuccess,
        CancellationToken cancellationToken = default)
    {
        if (onSuccess is null)
        {
            return Failure.NullFailure(nameof(onSuccess));
        }

        IEither<T> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.TapAsync(onSuccess, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IEither<T>> OnFailureAsync<T>(
        this IEither<T> either,
        Func<Failure, Task> onFailure,
        CancellationToken cancellationToken = default)
    {
        if (onFailure is null)
        {
            return Failure.NullFailure(nameof(onFailure));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (either is Failure failure)
        {
            await onFailure(failure).ConfigureAwait(false);
        }

        return either;
    }

    public static Task<IEither<T>> OnFailureAsync<T>(
        this Task<IEither<T>> eitherTask,
        Action<Failure> onFailure,
        CancellationToken cancellationToken = default) =>
        onFailure is null
            ? Task.FromResult<IEither<T>>(Failure.NullFailure(nameof(onFailure)))
            : eitherTask.OnFailureAsync(
                onFailure: failure =>
                {
                    onFailure(failure);
                    return Task.CompletedTask;
                },
                cancellationToken: cancellationToken);

    public static async Task<IEither<T>> OnFailureAsync<T>(
        this Task<IEither<T>> eitherTask,
        Func<Failure, Task> onFailure,
        CancellationToken cancellationToken = default)
    {
        if (onFailure is null)
        {
            return Failure.NullFailure(nameof(onFailure));
        }

        IEither<T> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.OnFailureAsync(onFailure, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IEither<T>> FilterAsync<T>(
        this IEither<T> either,
        Func<T, Task<bool>> predicate,
        Failure filterFailure,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            return Failure.NullFailure(nameof(predicate));
        }

        if (filterFailure is null)
        {
            return Failure.NullFailure(nameof(filterFailure));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return either switch
        {
            Ok<T> ok when await predicate(ok.Value).ConfigureAwait(false) => ok,
            Ok<T> => filterFailure,
            Failure failure => failure
        };
    }

    public static async Task<IEither<T>> FilterAsync<T>(
        this Task<IEither<T>> eitherTask,
        Func<T, Task<bool>> predicate,
        Failure filterFailure,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
        {
            return Failure.NullFailure(nameof(predicate));
        }

        if (filterFailure is null)
        {
            return Failure.NullFailure(nameof(filterFailure));
        }

        IEither<T> either = await AwaitEitherAsync(eitherTask, cancellationToken).ConfigureAwait(false);
        return await either.FilterAsync(predicate, filterFailure, cancellationToken).ConfigureAwait(false);
    }

    public static Task<IEither<T>> FilterAsync<T>(
        this Task<IEither<T>> eitherTask,
        Func<T, bool> predicate,
        Failure filterFailure,
        CancellationToken cancellationToken = default) =>
        predicate is null
            ? Task.FromResult<IEither<T>>(Failure.NullFailure(nameof(predicate)))
            : eitherTask.FilterAsync(
                predicate: value => Task.FromResult(predicate(value)),
                filterFailure: filterFailure,
                cancellationToken: cancellationToken);
}
