namespace WTorricos.Either.UnitTests;

public class IEitherExtensionsTest
{
    [Fact(DisplayName = "Map transforms Ok values")]
    public void MapTransformsOkValues()
    {
        IEither<int> either = new Ok<int>(2);

        IEither<int> result = either.Map(x => x * 3);

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(6, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "FlatMap short-circuits on Failure")]
    public void FlatMapShortCircuitsOnFailure()
    {
        IEither<int> either = new Failure("ERR", "Failed", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = IEitherExtensions.FlatMap<int, int>(either, x => new Ok<int>(x * 2));

        switch (result)
        {
            case Failure:
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Flatten unwraps nested Either")]
    public void FlattenUnwrapsNestedEither()
    {
        IEither<IEither<int>> nested = new Ok<IEither<int>>(new Ok<int>(7));

        IEither<int> result = nested.Flatten();

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(7, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "Where returns Failure when predicate fails")]
    public void WhereReturnsFailureWhenPredicateFails()
    {
        IEither<int> either = new Ok<int>(5);

        IEither<int> result = either.Where(x => x > 10);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("PREDICATE_FAILED", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "GetValueOrThrow throws on Failure")]
    public void GetValueOrThrowThrowsOnFailure()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => either.GetValueOrThrow());
        Assert.Contains("Boom", exception.Message);
    }

    [Fact(DisplayName = "GetValueOrThrow returns success value")]
    public void GetValueOrThrowReturnsSuccessValue()
    {
        IEither<int> either = new Ok<int>(42);

        int result = either.GetValueOrThrow();

        Assert.Equal(42, result);
    }

    [Fact(DisplayName = "MapFailure transforms Failure")]
    public void MapFailureTransformsFailure()
    {
        IEither<int> either = new Failure("ERR1", "Original", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = either.MapFailure(f => new Failure("ERR2", "Transformed", Severity.Error, DateTime.UtcNow, []));

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR2", failure.ErrorCode);
                Assert.Equal("Transformed", failure.Message);
                Assert.Equal(Severity.Error, failure.Level);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "MapFailure returns Ok unchanged")]
    public void MapFailureReturnsOkUnchanged()
    {
        IEither<int> either = new Ok<int>(10);

        IEither<int> result = either.MapFailure(f => new Failure("ERR", "New", Severity.Error, DateTime.UtcNow, []));

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

    [Fact(DisplayName = "Inspect invokes success callback")]
    public void InspectInvokesSuccessCallback()
    {
        IEither<int> either = new Ok<int>(5);
        int observed = 0;

        IEither<int> result = either.Inspect(onSuccess: value => observed = value);

        Assert.Equal(5, observed);
        switch (result)
        {
            case Ok<int>:
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "Inspect invokes failure callback")]
    public void InspectInvokesFailureCallback()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        string? observed = null;

        IEither<int> result = either.Inspect(onFailure: failure => observed = failure.ErrorCode);

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

    [Fact(DisplayName = "Inspect returns original Either unchanged")]
    public void InspectReturnsOriginalEitherUnchanged()
    {
        IEither<int> either = new Ok<int>(7);

        IEither<int> result = either.Inspect();

        Assert.Equal(either, result);
    }

    [Fact(DisplayName = "Filter returns failure when predicate false")]
    public void FilterReturnsFailureWhenPredicateFalse()
    {
        IEither<int> either = new Ok<int>(3);
        Failure filterFailure = new("SMALL", "Too small", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = either.Filter(x => x > 10, filterFailure);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("SMALL", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Filter returns Ok when predicate true")]
    public void FilterReturnsOkWhenPredicateTrue()
    {
        IEither<int> either = new Ok<int>(15);
        Failure filterFailure = new("SMALL", "Too small", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = either.Filter(x => x > 10, filterFailure);

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

    [Fact(DisplayName = "Filter returns Failure unchanged")]
    public void FilterReturnsFailureUnchanged()
    {
        IEither<int> either = new Failure("ERR", "Original", Severity.Error, DateTime.UtcNow, []);
        Failure filterFailure = new("SMALL", "Too small", Severity.Warning, DateTime.UtcNow, []);

        IEither<int> result = either.Filter(x => x > 10, filterFailure);

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

    [Fact(DisplayName = "FromNullable converts non-null to Ok")]
    public void FromNullableConvertsNonNullToOk()
    {
        string? value = "hello";
        Failure whenNull = new("NULL", "Was null", Severity.Error, DateTime.UtcNow, []);

        IEither<string> result = IEitherExtensions.FromNullable(value, whenNull);

        switch (result)
        {
            case Ok<string> ok:
                Assert.Equal("hello", ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<string>");
                break;
        }
    }

    [Fact(DisplayName = "FromNullable converts null to Failure")]
    public void FromNullableConvertsNullToFailure()
    {
        string? value = null;
        Failure whenNull = new("NULL", "Was null", Severity.Error, DateTime.UtcNow, []);

        IEither<string> result = IEitherExtensions.FromNullable(value, whenNull);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("NULL", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "FromNullable converts nullable struct to Ok")]
    public void FromNullableConvertsNullableStructToOk()
    {
        int? value = 10;
        Failure whenNull = new("NULL", "Was null", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = IEitherExtensions.FromNullable(value, whenNull);

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

    [Fact(DisplayName = "FromNullable converts null nullable struct to Failure")]
    public void FromNullableConvertsNullNullableStructToFailure()
    {
        int? value = null;
        Failure whenNull = new("NULL", "Was null", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = IEitherExtensions.FromNullable(value, whenNull);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("NULL", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Match folds success and failure")]
    public void MatchFoldsSuccessAndFailure()
    {
        IEither<int> okEither = new Ok<int>(7);
        IEither<int> failureEither = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        string okText = okEither.Match(
            onSuccess: value => $"ok:{value}",
            onFailure: failure => $"error:{failure.ErrorCode}");

        string failureText = failureEither.Match(
            onSuccess: value => $"ok:{value}",
            onFailure: failure => $"error:{failure.ErrorCode}");

        Assert.Equal("ok:7", okText);
        Assert.Equal("error:ERR", failureText);
    }

    [Fact(DisplayName = "Tap executes success callback only")]
    public void TapExecutesSuccessCallbackOnly()
    {
        IEither<int> either = new Ok<int>(4);
        int observed = 0;

        IEither<int> result = either.Tap(value => observed = value);

        Assert.Equal(4, observed);
        Assert.Equal(either, result);
    }

    [Fact(DisplayName = "OnFailure executes failure callback only")]
    public void OnFailureExecutesFailureCallbackOnly()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        string? observed = null;

        IEither<int> result = either.OnFailure(failure => observed = failure.ErrorCode);

        Assert.Equal("ERR", observed);
        Assert.Equal(either, result);
    }

    [Fact(DisplayName = "Map returns null failure for null map function")]
    public void MapReturnsNullFailureForNullMapFunction()
    {
        IEither<int> either = new Ok<int>(1);
        Func<int, int>? map = null;

        IEither<int> result = either.Map(map!);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("NULL_FAILURE", failure.ErrorCode);
                Assert.Equal("Required parameter 'map' was null.", failure.Message);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Filter returns null failure for null predicate")]
    public void FilterReturnsNullFailureForNullPredicate()
    {
        IEither<int> either = new Ok<int>(1);
        Failure filterFailure = new("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        Func<int, bool>? predicate = null;

        IEither<int> result = either.Filter(predicate!, filterFailure);

        AssertNullFailure(result, "predicate");
    }

    [Fact(DisplayName = "Sync extension null inputs return null failures")]
    public void SyncExtensionNullInputsReturnNullFailures()
    {
        IEither<int> either = new Ok<int>(1);
        Failure filterFailure = new("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);
        Func<int, int>? map = null;
        Func<int, IEither<int>>? bind = null;
        Func<Failure, Failure>? mapError = null;
        Action<int>? onSuccess = null;
        Action<Failure>? onFailure = null;
        Failure? nullFailure = null;

        AssertNullFailure(either.Map(map!), "map");
        AssertNullFailure(either.FlatMap(bind!), "bind");
        AssertNullFailure(either.MapFailure(mapError!), "mapError");
        AssertNullFailure(either.Tap(onSuccess!), "onSuccess");
        AssertNullFailure(either.OnFailure(onFailure!), "onFailure");
        AssertNullFailure(either.Filter(value => value > 0, nullFailure!), "filterFailure");
        AssertNullFailure(IEitherExtensions.FromNullable("ok", nullFailure!), "whenNull");
        AssertNullFailure(IEitherExtensions.FromNullable<int>(1, nullFailure!), "whenNull");
    }

    [Fact(DisplayName = "Void converts Ok to Unit")]
    public void VoidConvertsOkToUnit()
    {
        IEither<int> either = new Ok<int>(42);

        IEither<Unit> result = either.Void();

        switch (result)
        {
            case Ok<Unit>:
                break;
            default:
                Assert.Fail("Expected Ok<Unit>");
                break;
        }
    }

    [Fact(DisplayName = "Void preserves Failure")]
    public void VoidPreservesFailure()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        IEither<Unit> result = either.Void();

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

    [Fact(DisplayName = "Flatten unwraps nested Failure")]
    public void FlattenUnwrapsNestedFailure()
    {
        Failure innerFailure = new("ERR", "Inner", Severity.Error, DateTime.UtcNow, []);
        IEither<IEither<int>> nested = new Ok<IEither<int>>(innerFailure);

        IEither<int> result = nested.Flatten();

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

    [Fact(DisplayName = "Flatten returns Failure unchanged")]
    public void FlattenReturnsFailureUnchanged()
    {
        Failure outerFailure = new("ERR", "Outer", Severity.Error, DateTime.UtcNow, []);
        IEither<IEither<int>> nested = outerFailure;

        IEither<int> result = nested.Flatten();

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

    private static void AssertNullFailure<T>(IEither<T> result, string parameterName)
    {
        switch (result)
        {
            case Failure failure:
                Assert.Equal("NULL_FAILURE", failure.ErrorCode);
                Assert.Equal($"Required parameter '{parameterName}' was null.", failure.Message);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }
}
