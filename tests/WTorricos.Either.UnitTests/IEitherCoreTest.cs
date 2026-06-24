namespace WTorricos.Either.UnitTests;

public class IEitherCoreTest
{
    [Fact(DisplayName = "Ok<T> creation and value retrieval")]
    public void OkCreationSuccess()
    {
        const int expectedValue = 42;
        Ok<int> ok = new Ok<int>(expectedValue);

        Assert.Equal(expectedValue, ok.Value);
    }

    [Fact(DisplayName = "Ok<string> with various types")]
    public void OkWorksWithAnyType()
    {
        Ok<string> okString = new Ok<string>("hello");
        Ok<List<int>> okList = new Ok<List<int>>(new List<int> { 1, 2, 3 });
        Ok<object> okClass = new Ok<object>(new { name = "test" });

        Assert.Equal("hello", okString.Value);
        Assert.Equal(3, okList.Value.Count);
        Assert.NotNull(okClass.Value);
    }

    [Fact(DisplayName = "Failure creation with required fields")]
    public void FailureCreationWithRequiredFields()
    {
        const string errorCode = "TEST_ERROR";
        const string message = "Something went wrong";
        Severity severity = Severity.Error;
        DateTime timestamp = DateTime.UtcNow;
        List<Detail> details = new List<Detail>();

        Failure failure = new Failure(errorCode, message, severity, timestamp, details);

        Assert.Equal(errorCode, failure.ErrorCode);
        Assert.Equal(message, failure.Message);
        Assert.Equal(Severity.Error, failure.Level);
        Assert.Equal(timestamp, failure.Timestamp);
        Assert.Empty(failure.Details);
    }

    [Fact(DisplayName = "Failure with optional fields")]
    public void FailureWithOptionalFields()
    {
        Failure failure = new Failure(
            ErrorCode: "NOT_FOUND",
            Message: "Resource not found",
            Level: Severity.Warning,
            Timestamp: DateTime.UtcNow,
            Details: [new Detail("CODE_001", "Details about the error")],
            TraceId: "trace-123",
            StackTrace: "at Program.Main()",
            InnerError: null,
            Metadata: new Dictionary<string, object> { { "userId", 42 } }
        );

        Assert.Equal("NOT_FOUND", failure.ErrorCode);
        Assert.Equal("trace-123", failure.TraceId);
        Assert.Equal("at Program.Main()", failure.StackTrace);
        Assert.Single(failure.Details);
        Assert.True(failure.Metadata?.ContainsKey("userId"));
    }

    [Fact(DisplayName = "Union pattern matching on Ok")]
    public void UnionPatternMatchingOk()
    {
        IEither<int> either = new Ok<int>(42);

        int result = either switch
        {
            Ok<int> ok => ok.Value * 2,
            Failure err => 0
        };

        Assert.Equal(84, result);
    }

    [Fact(DisplayName = "Union pattern matching on Failure")]
    public void UnionPatternMatchingFailure()
    {
        Failure failure = new Failure("ERR_001", "Test error", Severity.Error, DateTime.UtcNow, []);
        IEither<int> either = failure;

        int result = either switch
        {
            Ok<int> ok => ok.Value,
            Failure err => -1
        };

        Assert.Equal(-1, result);
    }

    [Fact(DisplayName = "Failure.GetDisplayMessage with no details")]
    public void FailureDisplayMessageNoDetails()
    {
        Failure failure = new Failure(
            "ERR_001",
            "Main error message",
            Severity.Error,
            DateTime.UtcNow,
            new List<Detail>()
        );

        string displayMessage = failure.GetDisplayMessage();

        Assert.Equal("Main error message", displayMessage);
    }

    [Fact(DisplayName = "Failure.GetDisplayMessage with details")]
    public void FailureDisplayMessageWithDetails()
    {
        List<Detail> details = new List<Detail>
        {
            new Detail("DETAIL_1", "First detail"),
            new Detail("DETAIL_2", "Second detail")
        };

        Failure failure = new Failure(
            "ERR_001",
            "Main error",
            Severity.Error,
            DateTime.UtcNow,
            details
        );

        string displayMessage = failure.GetDisplayMessage();

        Assert.Contains("Main error", displayMessage);
        Assert.Contains("DETAIL_1: First detail", displayMessage);
        Assert.Contains("DETAIL_2: Second detail", displayMessage);
    }

    [Fact(DisplayName = "Failure with inner error chaining")]
    public void FailureWithInnerErrorChaining()
    {
        Failure innerFailure = new Failure(
            "INNER_ERR",
            "Inner error message",
            Severity.Warning,
            DateTime.UtcNow,
            new List<Detail>()
        );

        Failure outerFailure = new Failure(
            "OUTER_ERR",
            "Outer error message",
            Severity.Error,
            DateTime.UtcNow,
            new List<Detail>(),
            InnerError: innerFailure
        );

        Assert.NotNull(outerFailure.InnerError);
        Assert.Equal("Inner error message", outerFailure.InnerError?.Message);
    }

    [Fact(DisplayName = "Failure error chaining display message")]
    public void FailureChainedDisplayMessage()
    {
        Failure innerFailure = new Failure(
            "INNER",
            "Inner error",
            Severity.Error,
            DateTime.UtcNow,
            new List<Detail>()
        );

        Failure outerFailure = new Failure(
            "OUTER",
            "Outer error",
            Severity.Critical,
            DateTime.UtcNow,
            new List<Detail>(),
            InnerError: innerFailure
        );

        string displayMessage = outerFailure.GetDisplayMessage();

        Assert.Contains("Outer error", displayMessage);
        Assert.Contains("Inner Error: Inner error", displayMessage);
    }

    [Fact(DisplayName = "Failure with metadata")]
    public void FailureWithMetadata()
    {
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "userId", 12345 },
            { "operationId", "op-uuid" },
            { "timestamp", DateTime.UtcNow }
        };

        Failure failure = new Failure(
            "OP_FAILED",
            "Operation failed",
            Severity.Error,
            DateTime.UtcNow,
            new List<Detail>(),
            Metadata: metadata
        );

        Assert.NotNull(failure.Metadata);
        Assert.Equal(3, failure.Metadata.Count);
        Assert.Equal(12345, failure.Metadata!["userId"]);
        Assert.Equal("op-uuid", failure.Metadata["operationId"]);
    }

    [Fact(DisplayName = "Failure with TraceId for distributed tracing")]
    public void FailureWithTraceIdForDistributedTracing()
    {
        const string traceId = "0HN1GJ5V11L3D:00000001";
        Failure failure = new Failure(
            "DB_ERROR",
            "Database connection failed",
            Severity.Critical,
            DateTime.UtcNow,
            new List<Detail>(),
            TraceId: traceId
        );

        Assert.Equal(traceId, failure.TraceId);
    }

    [Fact(DisplayName = "Failure with stack trace")]
    public void FailureWithStackTrace()
    {
        string stackTrace = """
            at MyApp.Service.GetData() in /app/Service.cs:line 42
            at MyApp.Controller.Handle(Request request) in /app/Controller.cs:line 15
            """;

        Failure failure = new Failure(
            "UNHANDLED",
            "Unhandled exception",
            Severity.Critical,
            DateTime.UtcNow,
            new List<Detail>(),
            StackTrace: stackTrace
        );

        Assert.Contains("Service.GetData", failure.StackTrace);
        Assert.Contains("Controller.Handle", failure.StackTrace);
    }

    [Fact(DisplayName = "All severity levels")]
    public void AllSeverityLevels()
    {
        Failure infoFailure = new Failure("INFO", "Info", Severity.Info, DateTime.UtcNow, []);
        Failure warningFailure = new Failure("WARN", "Warning", Severity.Warning, DateTime.UtcNow, []);
        Failure errorFailure = new Failure("ERR", "Error", Severity.Error, DateTime.UtcNow, []);
        Failure criticalFailure = new Failure("CRIT", "Critical", Severity.Critical, DateTime.UtcNow, []);

        Assert.Equal(Severity.Info, infoFailure.Level);
        Assert.Equal(Severity.Warning, warningFailure.Level);
        Assert.Equal(Severity.Error, errorFailure.Level);
        Assert.Equal(Severity.Critical, criticalFailure.Level);
    }

    [Fact(DisplayName = "Failure timestamp is captured")]
    public void FailureTimestampCaptured()
    {
        DateTime beforeCreation = DateTime.UtcNow;
        Failure failure = new Failure("TEST", "Test", Severity.Error, DateTime.UtcNow, []);
        DateTime afterCreation = DateTime.UtcNow;

        Assert.True(failure.Timestamp >= beforeCreation);
        Assert.True(failure.Timestamp <= afterCreation);
    }

    [Fact(DisplayName = "Multiple detail codes")]
    public void MultipleDetailCodes()
    {
        List<Detail> details = new List<Detail>
        {
            new Detail("VALIDATION_NAME", "Name is required"),
            new Detail("VALIDATION_EMAIL", "Email format is invalid"),
            new Detail("VALIDATION_AGE", "Age must be greater than 18")
        };

        Failure failure = new Failure("VALIDATION_FAILED", "Validation failed", Severity.Warning, DateTime.UtcNow, details);

        Assert.Equal(3, failure.Details.Count);
        Assert.Contains(failure.Details, d => d.Code == "VALIDATION_NAME");
        Assert.Contains(failure.Details, d => d.Code == "VALIDATION_EMAIL");
        Assert.Contains(failure.Details, d => d.Code == "VALIDATION_AGE");
    }

    [Fact(DisplayName = "Details are immutable (read-only collection)")]
    public void DetailsAreImmutable()
    {
        List<Detail> detailList = new List<Detail> { new Detail("CODE", "Description") };
        Failure failure = new Failure("ERR", "Error", Severity.Error, DateTime.UtcNow, detailList);

        Assert.IsType<IReadOnlyList<Detail>>(failure.Details, exactMatch: false);
    }

    [Fact(DisplayName = "Ok and Failure can be used in collections")]
    public void OkAndFailureInCollections()
    {
        List<object> results = new List<object>
        {
            new Ok<int>(1),
            new Ok<string>("hello"),
            new Failure("ERR", "Error", Severity.Error, DateTime.UtcNow, [])
        };

        Assert.Equal(3, results.Count);
        Assert.Single(results.OfType<Ok<int>>());
        Assert.Single(results.OfType<Ok<string>>());
        Assert.Single(results.OfType<Failure>());
    }

    [Fact(DisplayName = "Unit.Instance is singleton")]
    public void UnitInstanceIsSingleton()
    {
        Unit instance1 = Unit.Instance;
        Unit instance2 = Unit.Instance;

        Assert.True(ReferenceEquals(instance1, instance2));
    }

    [Fact(DisplayName = "Unit.ToString returns unit notation")]
    public void UnitToStringReturnsUnitNotation()
    {
        Unit unit = Unit.Instance;

        Assert.Equal("()", unit.ToString());
    }

    [Fact(DisplayName = "Unit can be used in Ok")]
    public void UnitCanBeUsedInOk()
    {
        IEither<Unit> either = new Ok<Unit>(Unit.Instance);

        switch (either)
        {
            case Ok<Unit> ok:
                Assert.Equal(Unit.Instance, ok.Value);
                break;
            default:
                Assert.Fail("Expected Ok<Unit>");
                break;
        }
    }

    [Fact(DisplayName = "Unit can be used in Failure")]
    public void UnitCanBeUsedWithFailure()
    {
        Failure failure = new Failure("ERR", "Error", Severity.Error, DateTime.UtcNow, []);
        IEither<Unit> either = failure;

        switch (either)
        {
            case Failure f:
                Assert.Equal("ERR", f.ErrorCode);
                break;
            default:
                Assert.Fail("Expected Failure");
                break;
        }
    }
}
