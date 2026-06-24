# WTorricos.Either Migration Guide: v2 to v3

WTorricos.Either v3 is a clean rewrite built on C# union types. It is not API-compatible with v2.

## Type Mapping

| v2 | v3 |
|----|----|
| `IMaybe<T>` | `IEither<T>` |
| `Some<T>` | `Ok<T>` |
| `INone` / `INone<T>` | `Failure` |
| `NoneDetail` | `Detail` |

## Pattern Matching

### v2
```csharp
IMaybe<int> result = Maybe.Create(1);

switch (result)
{
    case Some<int> ok:
        Console.WriteLine(ok.Value);
        break;
    case INone error:
        Console.WriteLine(error.Message);
        break;
}
```

### v3
```csharp
IEither<int> result = new Ok<int>(1);

string output = result switch
{
    Ok<int> ok => ok.Value.ToString(),
    Failure error => error.Message
};
```

## Custom Errors

### v2
```csharp
[None]
public sealed partial record DivideByZeroError;
```

### v3
```csharp
public record DivideByZeroError(
    string Message = "Division by zero is not allowed",
    string? TraceId = null)
    : Failure(
        ErrorCode: "DIVIDE_BY_ZERO",
        Message,
        Level: Severity.Error,
        Timestamp: DateTime.UtcNow,
        Details: [],
        TraceId: TraceId);
```

## Fluent API

The v3 fluent API keeps the same overall shape:

```csharp
var result = new Ok<int>(2)
    .Map(x => x * 2)
    .FlatMap(x => new Ok<string>(x.ToString()));
```

## Async

Use the new Async suffix methods and ValueTask:

```csharp
var result = await either.MapAsync(x => ValueTask.FromResult(x + 1), cancellationToken);
```

## Notes

- v3 targets net11.0 preview tooling in this repository.
- The old source generator and `[None]` attribute are removed.
- `Failure` now carries richer diagnostic context.
