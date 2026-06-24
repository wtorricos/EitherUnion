# WTorricos.Either v2 → v3 Breaking Changes Summary

This document outlines all the breaking changes between WTorricos.Either v2 and v3.

## Major Changes

### 1. Type Renames

| v2 | v3 | Notes |
|----|----|-------|
| `IMaybe<T>` | `IEither<T>` | Core union type (now uses C# union syntax) |
| `Some<T>` | `Ok<T>` | Success/happy-path type |
| `Some` (non-generic) | N/A | No non-generic version in v3 |
| `INone<T>` | `Failure` | Error type (no longer interface, now record) |
| `INone` (non-generic) | N/A | N/A |

### 2. Union Types vs Interfaces

**v2:** Interface-based pattern matching
```csharp
IMaybe<int> result = ...;
var outcome = result.Match(
    value => $"Success: {value}",
    error => $"Error: {error.Message}"
);
```

**v3:** Union-based pattern matching
```csharp
IEither<int> result = ...;
var outcome = result switch
{
    Ok<int> ok => $"Success: {ok.Value}",
    Failure err => $"Error: {err.Message}"
};
```

### 3. Error Structure

**v2:** Interface with basic fields
```csharp
public interface INone : IMaybe
{
    string Message { get; }
    IReadOnlyCollection<NoneDetail> Details { get; }
    string GetDisplayMessage();
    IMaybe<TOut> Cast<TOut>();
}
```

**v3:** Rich record with context fields
```csharp
public record Failure(
    string ErrorCode,
    string Message,
    Severity Level,
    DateTime Timestamp,
    IReadOnlyList<Detail> Details,
    string? TraceId = null,
    string? StackTrace = null,
    Failure? InnerError = null,
    Dictionary<string, object>? Metadata = null
);
```

**New v3 Error Features:**
- `ErrorCode` — Machine-readable error identifier
- `Level` — Severity enumeration (Info, Warning, Error, Critical)
- `Timestamp` — When the error occurred
- `TraceId` — Distributed tracing identifier (optional)
- `StackTrace` — Exception stack trace (optional)
- `InnerError` — Error chaining / inner exceptions (optional)
- `Metadata` — Custom contextual data (e.g., userId, operationId)

### 4. Source Generator Removal

**v2:** Custom errors defined with `[None]` attribute
```csharp
[None]
public sealed partial record ValidationError;
```

**v3:** Custom errors are simple record inheritance
```csharp
public record ValidationError(
    string Message = "Validation failed",
    Severity Level = Severity.Error,
    string? TraceId = null
) : Failure(
    ErrorCode: "VALIDATION_FAILED",
    Message,
    Level,
    DateTime.UtcNow,
    Details: [],
    TraceId,
    StackTrace: null,
    InnerError: null,
    Metadata: null
);
```

### 5. Async Pattern Changes

**v2:** Separate sync/async overloads (Map, MapAsync, etc.)
```csharp
// Sync
IMaybe<int> result = maybe.Map(x => x + 1);

// Async (multiple overloads per method)
IMaybe<int> result = await maybe.Map(x => GetValueAsync(x));
Task<IMaybe<int>> result = await maybe.MapAsync(x => GetValueAsync(x));
```

**v3:** ValueTask + Async suffix pattern (no overload bloat)
```csharp
// Sync
IEither<int> result = either.Map(x => x + 1);

// Async
ValueTask<IEither<int>> result = either.MapAsync(x => GetValueAsync(x), cancellationToken);
```

### 6. API Surface Changes

| v2 Method | v3 Equivalent | Status |
|-----------|---------------|--------|
| `Map<TIn, TOut>` | `Map<TIn, TOut>` | ✓ Same |
| `FlatMap` / `SelectMany` | `FlatMap` | ✓ Same |
| `Flatten` | `Flatten` | ✓ Same |
| `Match` (various overloads) | `switch` expression | ❌ Removed in v3 |
| `Action` | N/A | ❌ Removed in v3 |
| `GetValueOrThrow` | `GetValueOrThrow` | ✓ Same |
| `Cast<T>` | N/A | ❌ Removed (union handles it) |

### 7. Non-Generic Version Removed

**v2:** Non-generic `IMaybe` and `Some`
```csharp
IMaybe result = Maybe.Create();
```

**v3:** Only generic `IEither<T>`
```csharp
// No non-generic version
// Use IEither<Unit> for operations without return value
IEither<Unit> result = ...;
```

### 8. Target Framework

| v2 | v3 |
|----|-----|
| `netstandard2.0` | `net11.0` |

v3 requires .NET 11 (preview) for C# union type support.

## Migration Path

### Step 1: Rename Types
- Replace `IMaybe<T>` with `IEither<T>`
- Replace `Some<T>` with `Ok<T>`
- Replace `INone<T>` / `INone` with `Failure`

### Step 2: Update Pattern Matching
```csharp
// v2
result.Match(
    value => UseValue(value),
    error => HandleError(error)
);

// v3
_ = result switch
{
    Ok<int> ok => UseValue(ok.Value),
    Failure err => HandleError(err)
};
```

### Step 3: Rewrite Custom Errors
```csharp
// v2
[None]
public sealed partial record DbError;

// v3
public record DbError(
    string Message = "Database error",
    Severity Level = Severity.Error,
    string? TraceId = null
) : Failure("DB_ERROR", Message, Level, DateTime.UtcNow, [], TraceId);
```

### Step 4: Update Async Code
```csharp
// v2
var result = await either.MapAsync(x => ProcessAsync(x));

// v3
var result = await either.MapAsync(x => ProcessAsync(x), cancellationToken);
```

## What Stayed the Same

- Core monadic operations (Map, FlatMap, Flatten)
- LINQ query syntax support
- Fluent API composition
- Error detail collection (NoneDetail → Detail)
- Immutability principles

## What's New in v3

✨ **Union Types** — Native C# language feature for discriminated unions  
✨ **Rich Error Context** — ErrorCode, Severity, TraceId, StackTrace, Metadata  
✨ **Error Chaining** — InnerError field for nested exception handling  
✨ **Distributed Tracing** — Built-in TraceId support  
✨ **Modern Async** — ValueTask + CancellationToken patterns  
✨ **Simpler Custom Errors** — No generator boilerplate  

## No Direct v2 Compatibility

**v3 is NOT backward compatible with v2.** It's a clean rewrite. Your options:

1. **Upgrade to v3:** Migrate your code using this guide
2. **Stay on v2:** v2 remains available on NuGet (WTorricos.Either v2.x)
3. **Run both:** Use namespace aliasing if needed (not recommended)

## Questions?

See [MIGRATION.md](./MIGRATION.md) for detailed before/after examples and patterns.
