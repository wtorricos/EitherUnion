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
}
