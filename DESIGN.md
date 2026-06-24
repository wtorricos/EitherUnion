# v1 Design: Union Types & Rich Error Context

This document explains the design philosophy and architecture of WTorricos.Either v1.

## Philosophy

WTorricos.Either v1 embraces **C# 13 union types** as first-class constructs for type-safe, expressive error handling. The library uses discriminated unions to represent the two states of an operation: success (`Ok`) or failure (`Failure`).

### Why union types?

1. **Type Safety** — The compiler ensures all cases are handled
2. **Performance** — Avoids interface-based dispatch and keeps data model simple
3. **Clarity** — Code reads naturally: "Either Ok or Failure"
4. **Simplicity** — No generator boilerplate, just records and pattern matching

## Core Types

### IEither<T> — The union

```csharp
public union IEither<T>(Failure, Ok<T>);
```

A discriminated union that represents the outcome of an operation:
- **Ok<T>** — Operation succeeded with value of type T
- **Failure** — Operation failed with error information

### Ok<T> — Success

```csharp
public record Ok<T>(T Value);
```

Simple, immutable success type holding the operation result.

### Failure — Rich error context

```csharp
public record Failure(
    string ErrorCode,           // Machine-readable ID (e.g., "NOT_FOUND")
    string Message,             // Human-readable description
    Severity Level,             // Info | Warning | Error | Critical
    DateTime Timestamp,         // When error occurred (UTC)
    IReadOnlyList<Detail> Details,  // Code + description pairs
    string? TraceId = null,     // Distributed tracing ID
    string? StackTrace = null,  // Exception details
    Failure? InnerError = null, // Error chaining
    IReadOnlyDictionary<string, object>? Metadata = null  // Custom context
);
```

**Key Features:**

- **ErrorCode** — First-class error identifier for categorization
- **Severity** — Structured severity levels (not just messages)
- **Timestamp** — Automatic error timestamping for diagnostics
- **DateTimeOffset support** — Can be constructed with `DateTimeOffset` for timezone-safe timestamps
- **TraceId** — Integrates with distributed tracing (OpenTelemetry, etc.)
- **StackTrace** — Captures exception context
- **InnerError** — Error chaining for root cause analysis
- **Metadata** — Custom fields (userId, operationId, retry count, etc.)

### Detail — Error details

```csharp
public record Detail(string Code, string Description);
```

Structured error details (e.g., validation field + reason).

### Severity — Error levels

```csharp
public enum Severity
{
    Info,
    Warning,
    Error,
    Critical
}
```

Allows filtering/routing errors by severity in production systems.

## Usage patterns

### Creating success

```csharp
IEither<int> result = new Ok<int>(42);
```

### Creating errors

**Simple error:**
```csharp
var failure = new Failure(
    "NOT_FOUND",
    "User not found",
    Severity.Warning,
    DateTime.UtcNow,
    []
);
IEither<User> result = failure;
```

**With details:**
```csharp
var failure = new Failure(
    "VALIDATION_FAILED",
    "Validation failed",
    Severity.Error,
    DateTime.UtcNow,
    new List<Detail>
    {
        new Detail("EMAIL_INVALID", "Email format is incorrect"),
        new Detail("AGE_TOO_LOW", "Must be 18 or older")
    }
);
```

**With full context:**
```csharp
var failure = new Failure(
    "DB_TIMEOUT",
    "Database query timed out",
    Severity.Critical,
    DateTime.UtcNow,
    new List<Detail>
    {
        new Detail("QUERY_TIME", "Took 30 seconds"),
        new Detail("TABLE", "Users")
    },
    TraceId: "0HN1GJ5V11L3D:00000001",
    StackTrace: stackTrace,
    Metadata: new Dictionary<string, object>
    {
        { "userId", 123 },
        { "operationId", "op-456" },
        { "retryCount", 2 }
    }
);
```

### Pattern matching

```csharp
IEither<int> result = GetUser(id);

var outcome = result switch
{
    Ok<int> user => $"Found user {user.Value}",
    Failure err => err.GetDisplayMessage()
};
```

### Fluent composition

```csharp
IEither<User> user = GetUser(id);
IEither<string> email = user.Map(u => u.Email);
IEither<bool> verified = email.FlatMap(e => CheckEmailVerified(e));
```

### Error handling

```csharp
_ = result switch
{
    Ok<int> ok => Console.WriteLine($"Success: {ok.Value}"),
    Failure error => Console.WriteLine($"Error: {error.Message}")
};
```

### Chained errors

```csharp
try {
    var data = await FetchDataAsync();
}
catch (Exception ex)
{
    var innerFailure = new Failure("API_ERROR", ex.Message, Severity.Error, DateTime.UtcNow, []);
    var outerFailure = new Failure(
        "OPERATION_FAILED",
        "Failed to complete operation",
        Severity.Critical,
        DateTime.UtcNow,
        [],
        InnerError: innerFailure
    );
    return outerFailure;
}
```

### Distributed tracing integration

```csharp
var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

var failure = new Failure(
    "AUTH_FAILED",
    "Authentication failed",
    Severity.Error,
    DateTime.UtcNow,
    [],
    TraceId: traceId,
    Metadata: new Dictionary<string, object>
    {
        { "userId", userId },
        { "ip", Request.HttpContext.Connection.RemoteIpAddress }
    }
);
```

## Custom error types

Custom errors are simple record inheritance:

```csharp
public record NotFoundError(
    string Resource = "Resource",
    string? TraceId = null
) : Failure(
    ErrorCode: $"{Resource.ToUpper()}_NOT_FOUND",
    Message: $"{Resource} not found",
    Level: Severity.Warning,
    Timestamp: DateTime.UtcNow,
    Details: [],
    TraceId: TraceId,
    StackTrace: null,
    InnerError: null,
    Metadata: null
);
```

Usage:
```csharp
if (user == null)
    return new NotFoundError("User", traceId);
```

## Async patterns

v1 uses modern async patterns with **ValueTask** and **CancellationToken**:

```csharp
// Async method with cancellation support
ValueTask<IEither<T>> result = either.MapAsync(
    map: x => GetValueAsync(x),
    cancellationToken: token
);

// Fluent async chains
var result = await either
    .MapAsync(u => FetchProfileAsync(u.Id), ct)
    .Result.FlatMapAsync(p => ValidateProfileAsync(p), ct)
    .Result;
```

**Key benefits:**
- **Zero allocation** when operation completes synchronously (ValueTask)
- **Cancellation support** throughout the chain
- **No overload explosion** (sync and async are separate methods)

## API surface

### Core methods

- `Map<TOut>(Func<T, TOut>)` — Transform success value
- `FlatMap<TOut>(Func<T, IEither<TOut>>)` — Monadic bind
- `Flatten()` — Unwrap nested Either
- `GetValueOrThrow()` — Extract or throw

### Async methods (with Async suffix)

- `MapAsync(Func<T, ValueTask<TOut>>, CancellationToken)` — Async transform
- `FlatMapAsync(Func<T, ValueTask<IEither<TOut>>>, CancellationToken)` — Async bind
- `ActionAsync(Func<T, ValueTask>, ..., CancellationToken)` — Async execute
- `MatchAsync(Func<T, ValueTask<TResult>>, ..., CancellationToken)` — Async fold

### LINQ support

```csharp
IEither<int> result = from x in GetFirst()
                      where x > 0
                      from y in GetSecond(x)
                      select x + y;
```

Supports:
- `Select` (via `Map`)
- `SelectMany` (via `FlatMap`)
- `Where` (filtering with error on false)

## Error propagation

- **Short-circuit on first error** — Chains stop immediately on Failure
- **Error context preserved** — Message, code, severity all retained
- **Chainable errors** — InnerError field for nested exception handling

## Performance considerations

- **Union modeling** keeps success/error handling explicit
- **ValueTask** helps reduce async allocations in synchronous completion paths
- **Pattern matching** keeps branching explicit and compiler-checked

## Testing

Core types are tested in `IEitherCoreTest.cs`:
- Union pattern matching
- Error display messages
- Error chaining
- Metadata attachment
- Severity levels
- Timestamp handling

Extension methods are tested in:
- Fluent API chains
- Async methods with cancellation
- LINQ query expressions

## Future enhancements

Potential additions in v1 maintenance releases:

- **Error retry policies** — Metadata-driven retry strategies
- **Error aggregation** — Collecting multiple errors
- **Either.All()** — Combining multiple `IEither<T>` values
- **Async iterables** — IAsyncEnumerable<IEither<T>>

---

**This is the first release line (v1), so no migration path is required yet.**
