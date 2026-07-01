namespace WTorricos.Either.UnitTests;

public class IEitherAsyncExtensionsTest
{
    [Fact(DisplayName = "MapAsync transforms successful values")]
    public async Task MapAsyncTransformsSuccessfulValues()
    {
        IEither<int> either = new Ok<int>(5);

        IEither<int> result = await either.MapAsync(value => ValueTask.FromResult(value * 2));

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(10, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "FlatMapAsync short-circuits failures")]
    public async Task FlatMapAsyncShortCircuitsFailures()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = await either.FlatMapAsync(value => ValueTask.FromResult<IEither<int>>(new Ok<int>(value + 1)));

        switch (result)
        {
            case Failure:
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "MatchAsync folds asynchronously")]
    public async Task MatchAsyncFoldsAsynchronously()
    {
        IEither<int> either = new Ok<int>(3);

        string result = await either.MatchAsync(
            value => ValueTask.FromResult($"ok:{value}"),
            failure => ValueTask.FromResult($"error:{failure.ErrorCode}"));

        Assert.Equal("ok:3", result);
    }

    [Fact(DisplayName = "ActionAsync executes success branch")]
    public async Task ActionAsyncExecutesSuccessBranch()
    {
        IEither<int> either = new Ok<int>(8);
        int observed = 0;

        await either.ActionAsync(value =>
        {
            observed = value;
            return ValueTask.CompletedTask;
        });

        Assert.Equal(8, observed);
    }

    [Fact(DisplayName = "ActionAsync executes failure branch")]
    public async Task ActionAsyncExecutesFailureBranch()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        string? observed = null;

        await either.ActionAsync(
            value => ValueTask.CompletedTask,
            onFailure: failure =>
            {
                observed = failure.ErrorCode;
                return ValueTask.CompletedTask;
            });

        Assert.Equal("ERR", observed);
    }

    [Fact(DisplayName = "ActionAsync handles Failure without onFailure")]
    public async Task ActionAsyncHandlesFailureWithoutOnFailure()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        int observed = 0;

        // Should not throw or call onSuccess
        await either.ActionAsync(value =>
        {
            observed = value;
            return ValueTask.CompletedTask;
        });

        Assert.Equal(0, observed);
    }

    [Fact(DisplayName = "MapAsync returns Failure unchanged")]
    public async Task MapAsyncReturnsFailureUnchanged()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = await either.MapAsync(value => ValueTask.FromResult(value * 2));

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "FlatMapAsync unwraps nested failure")]
    public async Task FlatMapAsyncUnwrapsNestedFailure()
    {
        IEither<int> either = new Ok<int>(5);

        IEither<int> result = await either.FlatMapAsync(value =>
            ValueTask.FromResult<IEither<int>>(new Failure("ERR", "Nested", Severity.Error, DateTime.UtcNow, [])));

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "MatchAsync handles Failure")]
    public async Task MatchAsyncHandlesFailure()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        string result = await either.MatchAsync(
            value => ValueTask.FromResult($"ok:{value}"),
            failure => ValueTask.FromResult($"error:{failure.ErrorCode}"));

        Assert.Equal("error:ERR", result);
    }

    [Fact(DisplayName = "MapAsync respects cancellation token")]
    public async Task MapAsyncRespectsCancellationToken()
    {
        IEither<int> either = new Ok<int>(5);
        CancellationTokenSource cts = new();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await either.MapAsync(value => ValueTask.FromResult(value * 2), cts.Token));
    }

    [Fact(DisplayName = "FlatMapAsync respects cancellation token")]
    public async Task FlatMapAsyncRespectsCancellationToken()
    {
        IEither<int> either = new Ok<int>(5);
        CancellationTokenSource cts = new();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await either.FlatMapAsync(value => ValueTask.FromResult<IEither<int>>(new Ok<int>(value + 1)), cts.Token));
    }

    [Fact(DisplayName = "MatchAsync respects cancellation token")]
    public async Task MatchAsyncRespectsCancellationToken()
    {
        IEither<int> either = new Ok<int>(5);
        CancellationTokenSource cts = new();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await either.MatchAsync(
                value => ValueTask.FromResult($"ok:{value}"),
                failure => ValueTask.FromResult($"error:{failure.ErrorCode}"),
                cts.Token));
    }

    [Fact(DisplayName = "ActionAsync respects cancellation token")]
    public async Task ActionAsyncRespectsCancellationToken()
    {
        IEither<int> either = new Ok<int>(5);
        CancellationTokenSource cts = new();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await either.ActionAsync(value => ValueTask.CompletedTask, cancellationToken: cts.Token));
    }

    [Fact(DisplayName = "MapAsync throws for null map function")]
    public async Task MapAsyncThrowsForNullMapFunction()
    {
        IEither<int> either = new Ok<int>(5);
        Func<int, ValueTask<int>>? map = null;

        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await either.MapAsync(map!));
    }

    [Fact(DisplayName = "MatchAsync throws for null failure callback")]
    public async Task MatchAsyncThrowsForNullFailureCallback()
    {
        IEither<int> either = new Ok<int>(5);
        Func<Failure, ValueTask<string>>? onFailure = null;

        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await either.MatchAsync(
                value => ValueTask.FromResult($"ok:{value}"),
                onFailure!));
    }

    [Fact(DisplayName = "MapAsync with Task transforms successful values")]
    public async Task MapAsyncTaskTransformsSuccessfulValues()
    {
        IEither<int> either = new Ok<int>(5);

        IEither<int> result = await either.MapAsync(value => Task.FromResult(value * 3));

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(15, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "FlatMapAsync with Task short-circuits failures")]
    public async Task FlatMapAsyncTaskShortCircuitsFailures()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = await either.FlatMapAsync(value => Task.FromResult<IEither<int>>(new Ok<int>(value + 1)));

        switch (result)
        {
            case Failure:
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "MatchAsync with Task folds asynchronously")]
    public async Task MatchAsyncTaskFoldsAsynchronously()
    {
        IEither<int> either = new Ok<int>(3);

        string result = await either.MatchAsync(
            value => Task.FromResult($"ok:{value}"),
            failure => Task.FromResult($"error:{failure.ErrorCode}"));

        Assert.Equal("ok:3", result);
    }

    [Fact(DisplayName = "MatchAsync with Task handles failure")]
    public async Task MatchAsyncTaskHandlesFailure()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        string result = await either.MatchAsync(
            value => Task.FromResult($"ok:{value}"),
            failure => Task.FromResult($"error:{failure.ErrorCode}"));

        Assert.Equal("error:ERR", result);
    }

    [Fact(DisplayName = "ActionAsync with Task executes success branch")]
    public async Task ActionAsyncTaskExecutesSuccessBranch()
    {
        IEither<int> either = new Ok<int>(8);
        int observed = 0;

        await either.ActionAsync(value =>
        {
            observed = value;
            return Task.CompletedTask;
        });

        Assert.Equal(8, observed);
    }

    [Fact(DisplayName = "ActionAsync with Task executes failure branch")]
    public async Task ActionAsyncTaskExecutesFailureBranch()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        string? observed = null;

        await either.ActionAsync(
            value => Task.CompletedTask,
            onFailure: failure =>
            {
                observed = failure.ErrorCode;
                return Task.CompletedTask;
            });

        Assert.Equal("ERR", observed);
    }

    [Fact(DisplayName = "ActionAsync with Task handles failure without onFailure")]
    public async Task ActionAsyncTaskHandlesFailureWithoutOnFailure()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        int observed = 0;

        await either.ActionAsync(value =>
        {
            observed = value;
            return Task.CompletedTask;
        });

        Assert.Equal(0, observed);
    }

    [Fact(DisplayName = "FlattenAsync unwraps Task nested Either")]
    public async Task FlattenAsyncUnwrapsTaskNestedEither()
    {
        IEither<Task<IEither<int>>> nested = new Ok<Task<IEither<int>>>(Task.FromResult<IEither<int>>(new Ok<int>(21)));

        IEither<int> result = await nested.FlattenAsync();

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(21, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "FlattenAsync returns failure unchanged")]
    public async Task FlattenAsyncReturnsFailureUnchanged()
    {
        IEither<Task<IEither<int>>> nested = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = await nested.FlattenAsync();

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "MapFailureAsync transforms Failure")]
    public async Task MapFailureAsyncTransformsFailure()
    {
        IEither<int> either = new Failure("ERR1", "Original", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = await either.MapFailureAsync(
            failure => Task.FromResult(new Failure("ERR2", $"{failure.Message} Changed", Severity.Error, DateTime.UtcNow, [])));

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR2", failure.ErrorCode);
                Assert.Equal("Original Changed", failure.Message);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "MapFailureAsync returns Ok unchanged")]
    public async Task MapFailureAsyncReturnsOkUnchanged()
    {
        IEither<int> either = new Ok<int>(9);

        IEither<int> result = await either.MapFailureAsync(
            failure => Task.FromResult(new Failure("ERR", failure.Message, failure.Level, DateTime.UtcNow, [])));

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(9, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "InspectAsync invokes success callback")]
    public async Task InspectAsyncInvokesSuccessCallback()
    {
        IEither<int> either = new Ok<int>(13);
        int observed = 0;

        IEither<int> result = await either.InspectAsync(onSuccess: value =>
        {
            observed = value;
            return Task.CompletedTask;
        });

        Assert.Equal(13, observed);
        Assert.Equal(either, result);
    }

    [Fact(DisplayName = "InspectAsync invokes failure callback")]
    public async Task InspectAsyncInvokesFailureCallback()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        string? observed = null;

        IEither<int> result = await either.InspectAsync(onFailure: failure =>
        {
            observed = failure.ErrorCode;
            return Task.CompletedTask;
        });

        Assert.Equal("ERR", observed);
        Assert.Equal(either, result);
    }

    [Fact(DisplayName = "InspectAsync returns original Either unchanged when callbacks are null")]
    public async Task InspectAsyncReturnsOriginalEitherUnchangedWhenCallbacksAreNull()
    {
        IEither<int> either = new Ok<int>(2);

        IEither<int> result = await either.InspectAsync();

        Assert.Equal(either, result);
    }

    [Fact(DisplayName = "TapAsync invokes success callback and preserves Either")]
    public async Task TapAsyncInvokesSuccessCallbackAndPreservesEither()
    {
        IEither<int> either = new Ok<int>(8);
        int observed = 0;

        IEither<int> result = await either.TapAsync(value =>
        {
            observed = value;
            return Task.CompletedTask;
        });

        Assert.Equal(8, observed);
        Assert.Equal(either, result);
    }

    [Fact(DisplayName = "OnFailureAsync invokes failure callback and preserves Either")]
    public async Task OnFailureAsyncInvokesFailureCallbackAndPreservesEither()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        string? observed = null;

        IEither<int> result = await either.OnFailureAsync(failure =>
        {
            observed = failure.ErrorCode;
            return Task.CompletedTask;
        });

        Assert.Equal("ERR", observed);
        Assert.Equal(either, result);
    }

    [Fact(DisplayName = "FilterAsync returns failure when predicate is false")]
    public async Task FilterAsyncReturnsFailureWhenPredicateIsFalse()
    {
        IEither<int> either = new Ok<int>(5);
        Failure filterFailure = new("FILTER_FAIL", "Invalid", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = await either.FilterAsync(
            predicate: value => Task.FromResult(value > 10),
            filterFailure: filterFailure);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("FILTER_FAIL", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "FilterAsync returns success when predicate is true")]
    public async Task FilterAsyncReturnsSuccessWhenPredicateIsTrue()
    {
        IEither<int> either = new Ok<int>(12);
        Failure filterFailure = new("FILTER_FAIL", "Invalid", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = await either.FilterAsync(
            predicate: value => Task.FromResult(value > 10),
            filterFailure: filterFailure);

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(12, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "FilterAsync returns existing failure unchanged")]
    public async Task FilterAsyncReturnsExistingFailureUnchanged()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        Failure filterFailure = new("FILTER_FAIL", "Invalid", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = await either.FilterAsync(
            predicate: value => Task.FromResult(value > 10),
            filterFailure: filterFailure);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Task-based chaining supports end-to-end fluent composition")]
    public async Task TaskBasedChainingSupportsEndToEndFluentComposition()
    {
        List<string> events = [];
        IEither<int> seed = new Ok<int>(10);
        Func<int, IEither<int>> increment = value => new Ok<int>(value + 1);
        Func<int, Task<IEither<int>>> doubleAsync = value => Task.FromResult<IEither<int>>(new Ok<int>(value * 2));

        IEither<string> result = await seed
            .OnFailure(failure => events.Add($"sync-failure:{failure.ErrorCode}"))
            .FlatMap(increment)
            .Inspect(onSuccess: _ => events.Add("sync-success"))
            .FlatMapAsync(doubleAsync)
            .OnFailureAsync(failure => events.Add($"async-failure:{failure.ErrorCode}"))
            .MapAsync(value => Task.FromResult(value + 3))
            .TapAsync(value => events.Add($"tap:{value}"))
            .MapAsync(value => $"ok:{value}")
            .InspectAsync(onSuccess: value => events.Add($"done:{value}"));

        switch (result)
        {
            case Ok<string> ok:
                Assert.Equal("ok:25", ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<string>");
                break;
        }

        Assert.Equal(["sync-success", "tap:25", "done:ok:25"], events);
    }

    [Fact(DisplayName = "MapAsync on Task source propagates failure")]
    public async Task MapAsyncOnTaskSourcePropagatesFailure()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));

        IEither<int> result = await eitherTask.MapAsync(value => Task.FromResult(value + 1));

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "FlatMapAsync on Task source short-circuits failures")]
    public async Task FlatMapAsyncOnTaskSourceShortCircuitsFailures()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));

        IEither<int> result = await eitherTask.FlatMapAsync(value => Task.FromResult<IEither<int>>(new Ok<int>(value + 1)));

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "InspectAsync on Task source invokes failure callback")]
    public async Task InspectAsyncOnTaskSourceInvokesFailureCallback()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));
        string? observed = null;

        IEither<int> result = await eitherTask.InspectAsync(onFailure: failure => observed = failure.ErrorCode);

        Assert.Equal("ERR", observed);
        switch (result)
        {
            case Failure:
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "TapAsync on Task source with Func preserves failure")]
    public async Task TapAsyncOnTaskSourceWithFuncPreservesFailure()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));
        int observed = 0;

        IEither<int> result = await eitherTask.TapAsync(value =>
        {
            observed = value;
            return Task.CompletedTask;
        });

        Assert.Equal(0, observed);
        switch (result)
        {
            case Failure:
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "OnFailureAsync on Task source with Func executes callback")]
    public async Task OnFailureAsyncOnTaskSourceWithFuncExecutesCallback()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));
        string? observed = null;

        IEither<int> result = await eitherTask.OnFailureAsync(failure =>
        {
            observed = failure.ErrorCode;
            return Task.CompletedTask;
        });

        Assert.Equal("ERR", observed);
        switch (result)
        {
            case Failure:
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Task-source async methods throw on null task")]
    public async Task TaskSourceAsyncMethodsThrowOnNullTask()
    {
        Task<IEither<int>>? eitherTask = null;

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => eitherTask!.MapAsync(value => Task.FromResult(value + 1)));
    }

    [Fact(DisplayName = "Task-source async methods respect cancellation token")]
    public async Task TaskSourceAsyncMethodsRespectCancellationToken()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(5));
        CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            eitherTask.MapAsync(value => Task.FromResult(value + 1), cancellationTokenSource.Token));
    }

    [Fact(DisplayName = "MatchAsync on Task source supports Task callbacks")]
    public async Task MatchAsyncOnTaskSourceSupportsTaskCallbacks()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(7));

        string result = await eitherTask.MatchAsync(
            onSuccess: value => Task.FromResult($"ok:{value}"),
            onFailure: failure => Task.FromResult($"error:{failure.ErrorCode}"));

        Assert.Equal("ok:7", result);
    }

    [Fact(DisplayName = "MatchAsync on Task source supports sync callbacks")]
    public async Task MatchAsyncOnTaskSourceSupportsSyncCallbacks()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));

        string result = await eitherTask.MatchAsync(
            onSuccess: value => $"ok:{value}",
            onFailure: failure => $"error:{failure.ErrorCode}");

        Assert.Equal("error:ERR", result);
    }

    [Fact(DisplayName = "MatchAsync on Task source with sync callbacks handles success")]
    public async Task MatchAsyncOnTaskSourceWithSyncCallbacksHandlesSuccess()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(11));

        string result = await eitherTask.MatchAsync(
            onSuccess: value => $"ok:{value}",
            onFailure: failure => $"error:{failure.ErrorCode}");

        Assert.Equal("ok:11", result);
    }

    [Fact(DisplayName = "ActionAsync on Task source supports action callbacks")]
    public async Task ActionAsyncOnTaskSourceSupportsActionCallbacks()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));
        string? observed = null;

        await eitherTask.ActionAsync(
            onSuccess: value => { },
            onFailure: failure => observed = failure.ErrorCode);

        Assert.Equal("ERR", observed);
    }

    [Fact(DisplayName = "ActionAsync on Task source supports Task callbacks")]
    public async Task ActionAsyncOnTaskSourceSupportsTaskCallbacks()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(6));
        int observed = 0;

        await eitherTask.ActionAsync(
            onSuccess: value =>
            {
                observed = value;
                return Task.CompletedTask;
            });

        Assert.Equal(6, observed);
    }

    [Fact(DisplayName = "FlattenAsync on Task source unwraps nested value")]
    public async Task FlattenAsyncOnTaskSourceUnwrapsNestedValue()
    {
        Task<IEither<Task<IEither<int>>>> eitherTask = Task.FromResult<IEither<Task<IEither<int>>>>(
            new Ok<Task<IEither<int>>>(Task.FromResult<IEither<int>>(new Ok<int>(44))));

        IEither<int> result = await eitherTask.FlattenAsync();

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(44, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "MapFailureAsync on Task source transforms failure")]
    public async Task MapFailureAsyncOnTaskSourceTransformsFailure()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR1", "Boom", Severity.Error, DateTime.UtcNow, []));

        IEither<int> result = await eitherTask.MapFailureAsync(
            failure => Task.FromResult(new Failure("ERR2", failure.Message, failure.Level, DateTime.UtcNow, [])));

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR2", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "InspectAsync on Task source supports async callbacks")]
    public async Task InspectAsyncOnTaskSourceSupportsAsyncCallbacks()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(9));
        int observed = 0;

        IEither<int> result = await eitherTask.InspectAsync(onSuccess: value =>
        {
            observed = value;
            return Task.CompletedTask;
        });

        Assert.Equal(9, observed);
        Assert.Equal(new Ok<int>(9), result);
    }

    [Fact(DisplayName = "FilterAsync on Task source supports Task predicate")]
    public async Task FilterAsyncOnTaskSourceSupportsTaskPredicate()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(12));
        Failure filterFailure = new("FILTER_FAIL", "Invalid", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = await eitherTask.FilterAsync(value => Task.FromResult(value > 10), filterFailure);

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(12, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "FilterAsync on Task source supports sync predicate")]
    public async Task FilterAsyncOnTaskSourceSupportsSyncPredicate()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(4));
        Failure filterFailure = new("FILTER_FAIL", "Invalid", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = await eitherTask.FilterAsync(value => value > 10, filterFailure);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("FILTER_FAIL", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "MatchAsync on Task source with sync callbacks throws for null success callback")]
    public async Task MatchAsyncOnTaskSourceWithSyncCallbacksThrowsForNullSuccessCallback()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(1));
        Func<int, string>? onSuccess = null;

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            eitherTask.MatchAsync(onSuccess!, failure => failure.ErrorCode));
    }

    [Fact(DisplayName = "MatchAsync on Task source with sync callbacks throws for null failure callback")]
    public async Task MatchAsyncOnTaskSourceWithSyncCallbacksThrowsForNullFailureCallback()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(1));
        Func<Failure, string>? onFailure = null;

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            eitherTask.MatchAsync(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture), onFailure!));
    }

    [Fact(DisplayName = "ActionAsync on Task source with Action callbacks executes success path")]
    public async Task ActionAsyncOnTaskSourceWithActionCallbacksExecutesSuccessPath()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(42));
        int observed = 0;

        await eitherTask.ActionAsync(onSuccess: value => observed = value);

        Assert.Equal(42, observed);
    }

    [Fact(DisplayName = "ActionAsync on Task source with Action callbacks supports null failure callback")]
    public async Task ActionAsyncOnTaskSourceWithActionCallbacksSupportsNullFailureCallback()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));

        await eitherTask.ActionAsync(onSuccess: _ => { }, onFailure: null);
    }

    [Fact(DisplayName = "ActionAsync on Task source with Action callbacks throws for null success callback")]
    public async Task ActionAsyncOnTaskSourceWithActionCallbacksThrowsForNullSuccessCallback()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(1));
        Action<int>? onSuccess = null;

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => eitherTask.ActionAsync(onSuccess!));
    }

    [Fact(DisplayName = "Task chain MapAsync FlatMapAsync FilterAsync MatchAsync returns transformed success")]
    public async Task TaskChainMapFlatMapFilterMatchReturnsTransformedSuccess()
    {
        Failure filterFailure = new("FILTER_FAIL", "Invalid", Severity.Warning, DateTime.UtcNow, []);
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(4));

        string result = await eitherTask
            .MapAsync(value => value + 1)
            .FlatMapAsync(value => Task.FromResult<IEither<int>>(new Ok<int>(value * 3)))
            .FilterAsync(value => value > 10, filterFailure)
            .MatchAsync(
                onSuccess: value => $"ok:{value}",
                onFailure: failure => $"error:{failure.ErrorCode}");

        Assert.Equal("ok:15", result);
    }

    [Fact(DisplayName = "Task chain short-circuits success callbacks and routes to failure path")]
    public async Task TaskChainShortCircuitsSuccessCallbacksAndRoutesToFailurePath()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []));
        bool mapExecuted = false;
        bool flatMapExecuted = false;
        bool tapExecuted = false;
        string? observedErrorCode = null;

        string result = await eitherTask
            .MapAsync(value =>
            {
                mapExecuted = true;
                return Task.FromResult(value + 1);
            })
            .FlatMapAsync(value =>
            {
                flatMapExecuted = true;
                return Task.FromResult<IEither<int>>(new Ok<int>(value * 2));
            })
            .TapAsync(value =>
            {
                tapExecuted = true;
                return Task.CompletedTask;
            })
            .OnFailureAsync(failure =>
            {
                observedErrorCode = failure.ErrorCode;
                return Task.CompletedTask;
            })
            .MatchAsync(
                onSuccess: value => $"ok:{value}",
                onFailure: failure => $"error:{failure.ErrorCode}");

        Assert.False(mapExecuted);
        Assert.False(flatMapExecuted);
        Assert.False(tapExecuted);
        Assert.Equal("ERR", observedErrorCode);
        Assert.Equal("error:ERR", result);
    }

    [Fact(DisplayName = "Task chain FilterAsync MapFailureAsync InspectAsync and MatchAsync transforms generated failure")]
    public async Task TaskChainFilterMapFailureInspectAndMatchTransformsGeneratedFailure()
    {
        Failure filterFailure = new("FILTER_FAIL", "Too small", Severity.Warning, DateTime.UtcNow, []);
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(2));
        string? inspectedCode = null;

        string result = await eitherTask
            .MapAsync(value => Task.FromResult(value + 1))
            .FilterAsync(value => value > 10, filterFailure)
            .MapFailureAsync(failure => Task.FromResult(new Failure($"MAPPED_{failure.ErrorCode}", failure.Message, failure.Level, DateTime.UtcNow, [])))
            .InspectAsync(onFailure: failure => inspectedCode = failure.ErrorCode)
            .MatchAsync(
                onSuccess: value => $"ok:{value}",
                onFailure: failure => $"error:{failure.ErrorCode}");

        Assert.Equal("MAPPED_FILTER_FAIL", inspectedCode);
        Assert.Equal("error:MAPPED_FILTER_FAIL", result);
    }

    [Fact(DisplayName = "Task nested chain FlattenAsync MapAsync FlatMapAsync TapAsync and MatchAsync returns expected value")]
    public async Task TaskNestedChainFlattenMapFlatMapTapAndMatchReturnsExpectedValue()
    {
        Task<IEither<Task<IEither<int>>>> eitherTask = Task.FromResult<IEither<Task<IEither<int>>>>(
            new Ok<Task<IEither<int>>>(Task.FromResult<IEither<int>>(new Ok<int>(5))));
        int observedFromTap = 0;

        string result = await eitherTask
            .FlattenAsync()
            .MapAsync(value => Task.FromResult(value * 4))
            .FlatMapAsync(value => Task.FromResult<IEither<int>>(new Ok<int>(value + 2)))
            .TapAsync(value =>
            {
                observedFromTap = value;
                return Task.CompletedTask;
            })
            .MatchAsync(
                onSuccess: value => $"ok:{value}",
                onFailure: failure => $"error:{failure.ErrorCode}");

        Assert.Equal(22, observedFromTap);
        Assert.Equal("ok:22", result);
    }

    [Fact(DisplayName = "Task chain ending with ActionAsync executes success callback after transformations")]
    public async Task TaskChainEndingWithActionAsyncExecutesSuccessCallbackAfterTransformations()
    {
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(2));
        int observedSuccess = 0;
        string? observedFailure = null;

        await eitherTask
            .MapAsync(value => Task.FromResult(value + 5))
            .FlatMapAsync(value => Task.FromResult<IEither<int>>(new Ok<int>(value * 2)))
            .ActionAsync(
                onSuccess: value =>
                {
                    observedSuccess = value;
                    return Task.CompletedTask;
                },
                onFailure: failure =>
                {
                    observedFailure = failure.ErrorCode;
                    return Task.CompletedTask;
                });

        Assert.Equal(14, observedSuccess);
        Assert.Null(observedFailure);
    }

    [Fact(DisplayName = "Task chain ending with ActionAsync executes failure callback after filter failure")]
    public async Task TaskChainEndingWithActionAsyncExecutesFailureCallbackAfterFilterFailure()
    {
        Failure filterFailure = new("FILTER_FAIL", "Invalid", Severity.Warning, DateTime.UtcNow, []);
        Task<IEither<int>> eitherTask = Task.FromResult<IEither<int>>(new Ok<int>(3));
        bool successExecuted = false;
        string? observedFailure = null;

        await eitherTask
            .MapAsync(value => value + 1)
            .FilterAsync(value => value > 20, filterFailure)
            .ActionAsync(
                onSuccess: _ =>
                {
                    successExecuted = true;
                    return Task.CompletedTask;
                },
                onFailure: failure =>
                {
                    observedFailure = failure.ErrorCode;
                    return Task.CompletedTask;
                });

        Assert.False(successExecuted);
        Assert.Equal("FILTER_FAIL", observedFailure);
    }
}
