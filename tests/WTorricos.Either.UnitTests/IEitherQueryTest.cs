namespace WTorricos.Either.UnitTests;

public class IEitherQueryTest
{
    [Fact(DisplayName = "LINQ query syntax works with IEither")]
    public void LinqQuerySyntaxWorksWithIEither()
    {
        IEither<int> first = new Ok<int>(2);
        IEither<int> second = new Ok<int>(3);

        IEither<int> result =
            from x in first
            from y in second
            select x + y;

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(5, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "LINQ Where fails with custom Failure")]
    public void LinqWhereFailsWithCustomFailure()
    {
        IEither<int> result = IEitherQueryExtensions.Where<int>(new Ok<int>(4),
            predicate: value => value > 10,
            failure: new Failure("TOO_SMALL", "Value is too small", Severity.Warning, DateTime.UtcNow, []));

        switch (result)
        {
            case Failure failure:
                Assert.Equal("TOO_SMALL", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Custom error types inherit from Failure")]
    public void CustomErrorTypesInheritFromFailure()
    {
        IEither<int> result = new DivideByZeroError();

        switch (result)
        {
            case Failure failure:
                Assert.Equal("DIVIDE_BY_ZERO", failure.ErrorCode);
                Assert.Equal("Division by zero is not allowed", failure.Message);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Select delegates to Map")]
    public void SelectDelegatesToMap()
    {
        IEither<int> either = new Ok<int>(3);

        IEither<int> result = either.Select(x => x * 2);

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

    [Fact(DisplayName = "Select propagates Failure")]
    public void SelectPropagatesFailure()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = either.Select(x => x * 2);

        switch (result)
        {
            case Failure:
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "SelectMany single overload delegates to FlatMap")]
    public void SelectManySingleOverloadDelegatesToFlatMap()
    {
        IEither<int> either = new Ok<int>(3);

        IEither<int> result = either.SelectMany<int, int>(x => new Ok<int>(x * 2));

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

    [Fact(DisplayName = "SelectMany single overload propagates Failure")]
    public void SelectManySingleOverloadPropagatesFailure()
    {
        IEither<int> either = new Failure("ERR", "Boom", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = either.SelectMany<int, int>(x => new Ok<int>(x * 2));

        switch (result)
        {
            case Failure:
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "SelectMany two overload composes operations")]
    public void SelectManyTwoOverloadComposesOperations()
    {
        IEither<int> first = new Ok<int>(2);

        IEither<int> result = first.SelectMany<int, int, int>(
            x => new Ok<int>(x * 3),
            (x, y) => x + y);

        switch (result)
        {
            case Ok<int> ok:
                Assert.Equal(8, ok.Value); // x=2, y=6, result=2+6=8
                break;
            default:
                Assert.Fail("Expected Ok<int>");
                break;
        }
    }

    [Fact(DisplayName = "SelectMany two overload propagates first Failure")]
    public void SelectManyTwoOverloadPropagatesFirstFailure()
    {
        IEither<int> first = new Failure("ERR1", "First", Severity.Error, DateTime.UtcNow, []);

        IEither<int> result = first.SelectMany<int, int, int>(
            x => new Ok<int>(x * 3),
            (x, y) => x + y);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("ERR1", failure.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "SelectMany two overload propagates second Failure")]
    public void SelectManyTwoOverloadPropagatesSecondFailure()
    {
        IEither<int> first = new Ok<int>(2);

        IEither<int> result = first.SelectMany<int, int, int>(
            x => new Failure("ERR2", "Second", Severity.Error, DateTime.UtcNow, []),
            (x, y) => x + y);

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

    [Fact(DisplayName = "Where uses default failure when none provided")]
    public void WhereUsesDefaultFailureWhenNoneProvided()
    {
        IEither<int> either = new Ok<int>(3);

        IEither<int> result = either.Where(x => x > 10);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("PREDICATE_FAILED", failure.ErrorCode);
                Assert.Equal("The LINQ predicate returned false.", failure.Message);
                Assert.Equal(Severity.Warning, failure.Level);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Where uses custom failure when provided")]
    public void WhereUsesCustomFailureWhenProvided()
    {
        IEither<int> either = new Ok<int>(3);
        Failure custom = new("CUSTOM", "Custom message", Severity.Critical, DateTime.UtcNow, []);

        IEither<int> result = either.Where(x => x > 10, custom);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("CUSTOM", failure.ErrorCode);
                Assert.Equal("Custom message", failure.Message);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "Where passes predicate when true")]
    public void WherePassesPredicateWhenTrue()
    {
        IEither<int> either = new Ok<int>(15);

        IEither<int> result = either.Where(x => x > 10);

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

    [Fact(DisplayName = "Where returns null failure when predicate is null")]
    public void WhereReturnsNullFailureWhenPredicateIsNull()
    {
        IEither<int> either = new Ok<int>(15);
        Func<int, bool>? predicate = null;

        IEither<int> result = either.Where(predicate!);

        switch (result)
        {
            case Failure failure:
                Assert.Equal("NULL_FAILURE", failure.ErrorCode);
                Assert.Equal("Required parameter 'predicate' was null.", failure.Message);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }

    [Fact(DisplayName = "SelectMany returns null failure when callbacks are null")]
    public void SelectManyReturnsNullFailureWhenCallbacksAreNull()
    {
        IEither<int> either = new Ok<int>(1);
        Func<int, IEither<int>>? collectionSelector = null;
        Func<int, int, int>? resultSelector = null;

        IEither<int> collectionSelectorResult = either.SelectMany(collectionSelector!, (x, y) => x + y);
        IEither<int> resultSelectorResult = either.SelectMany(x => new Ok<int>(x), resultSelector!);

        switch (collectionSelectorResult)
        {
            case Failure failure:
                Assert.Equal("NULL_FAILURE", failure.ErrorCode);
                Assert.Equal("Required parameter 'collectionSelector' was null.", failure.Message);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }

        switch (resultSelectorResult)
        {
            case Failure failure:
                Assert.Equal("NULL_FAILURE", failure.ErrorCode);
                Assert.Equal("Required parameter 'resultSelector' was null.", failure.Message);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }
}
