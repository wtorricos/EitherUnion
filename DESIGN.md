# v3 Design: Union Types & Rich Error Context

This document explains the design philosophy and architecture of Results v3.

## Philosophy

Results v3 embraces **C# 13 union types** as first-class constructs for type-safe, expressive error handling. Instead of interfaces and abstract types, v3 uses discriminated unions to represent the two states of an operation: success (`Ok`) or failure (`Failure`).

### Why Union Types?

1. **Type Safety** — The compiler ensures all cases are handled
2. **Performance** — Unions are stack-allocated, no boxing/interface indirection
3. **Clarity** — Code reads naturally: "Either Ok or Failure"
4. **Simplicity** — No generator boilerplate, just records and pattern matching

## Core Types

### IEither<T> — The Union

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

### Failure — Rich Error Context

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
    Dictionary<string, object>? Metadata = null  // Custom context
);
```

**Key Features:**

- **ErrorCode** — First-class error identifier for categorization
- **Severity** — Structured severity levels (not just messages)
- **Timestamp** — Automatic error timestamping for diagnostics
- **TraceId** — Integrates with distributed tracing (OpenTelemetry, etc.)
- **StackTrace** — Captures exception context
- **InnerError** — Error chaining for root cause analysis
- **Metadata** — Custom fields (userId, operationId, retry count, etc.)

### Detail — Error Details

```csharp
public record Detail(string Code, string Description);
```

Structured error details (e.g., validation field + reason).

### Severity — Error Levels

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

## Usage Patterns

### Creating Success

```csharp
IEither<int> result = new Ok<int>(42);
```

### Creating Errors

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

### Pattern Matching

```csharp
IEither<int> result = GetUser(id);

var outcome = result switch
{
    Ok<int> user => $"Found user {user.Value}",
    Failure err => err.GetDisplayMessage()
};
```

### Fluent Composition

```csharp
IEither<User> user = GetUser(id);
IEither<string> email = user.Map(u => u.Email);
IEither<bool> verified = email.FlatMap(e => CheckEmailVerified(e));
```

### Error Handling

```csharp
_ = result switch
{
    Ok<int> ok => Console.WriteLine($"Success: {ok.Value}"),
    Failure error => Console.WriteLine($"Error: {error.Message}")
};
```

### Chained Errors

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

### Distributed Tracing Integration

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

## Custom Error Types

Since v3 removes source generators, custom errors are simple record inheritance:

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

## Async Patterns

v3 uses modern async patterns with **ValueTask** and **CancellationToken**:

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

## API Surface

### Core Methods

- `Map<TOut>(Func<T, TOut>)` — Transform success value
- `FlatMap<TOut>(Func<T, IEither<TOut>>)` — Monadic bind
- `Flatten()` — Unwrap nested Either
- `GetValueOrThrow()` — Extract or throw

### Async Methods (with Async suffix)

- `MapAsync(Func<T, ValueTask<TOut>>, CancellationToken)` — Async transform
- `FlatMapAsync(Func<T, ValueTask<IEither<TOut>>>, CancellationToken)` — Async bind
- `ActionAsync(Func<T, ValueTask>, ..., CancellationToken)` — Async execute
- `MatchAsync(Func<T, ValueTask<TResult>>, ..., CancellationToken)` — Async fold

### LINQ Support

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

## Error Propagation

- **Short-circuit on first error** — Chains stop immediately on Failure
- **Error context preserved** — Message, code, severity all retained
- **Chainable errors** — InnerError field for nested exception handling

## Performance Considerations

- **Union types are stack-allocated** — No heap allocation like interface
- **ValueTask avoids allocation** — Zero alloc when sync path completes
- **Pattern matching is optimized** — Compiler generates efficient dispatch
- **No reflection** — All types known at compile time (unlike v2 generator)

## Comparison: v2 vs v3

| Aspect | v2 | v3 |
|--------|----|----|
| Core Type | Interface (IMaybe) | Union (IEither) |
| Success | Some<T> | Ok<T> |
| Error | INone (interface) | Failure (record) |
| Error Code Generation | Source generator | Manual record |
| Async Pattern | Multiple overloads | ValueTask + suffix |
| Error Context | Message + Details | Message + Code + Severity + TraceId + Metadata |
| Error Chaining | Not supported | InnerError field |
| Performance | Good | Better (unions are stack-allocated) |

## Testing

Core types are tested in `IEitherCoreTest.cs`:
- Union pattern matching
- Error display messages
- Error chaining
- Metadata attachment
- Severity levels
- Timestamp handling

Extension methods tested in Phase 2:
- Fluent API chains
- Async methods with cancellation
- LINQ query expressions

## Future Enhancements

Potential additions in v3 maintenance releases:

- **Error retry policies** — Metadata-driven retry strategies
- **Error aggregation** — Collecting multiple errors
- **Result.All()** — Combining multiple Results
- **Async iterables** — IAsyncEnumerable<IEither<T>>

---

**For migration from v2, see [BREAKING_CHANGES.md](./BREAKING_CHANGES.md).**
