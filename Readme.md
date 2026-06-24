# WTorricos.Either

WTorricos.Either is a .NET library for expressive, type-safe error handling with `IEither<T>`, `Ok<T>`, and `Failure`.

Its goal is to provide a practical union for success-or-failure workflows in modern .NET: while union support now exists in the platform, there is still no native built-in union tailored to this common error-handling shape.

## Highlights

- `IEither<T>` union with `Ok<T>` and `Failure`
- Rich failure context: `ErrorCode`, `Severity`, `TraceId`, `StackTrace`, `InnerError`, `Metadata`
- Fluent helpers: `Map`, `FlatMap`, `Flatten`, `MapFailure`, `Match`, `Inspect`, `Tap`, `OnFailure`, `Filter`, `Void`
- Result extraction helpers: `GetValueOrThrow`
- Async helpers: `MapAsync`, `FlatMapAsync`, `MatchAsync`, `ActionAsync`
- LINQ query syntax support through `Select`, `SelectMany`, and `Where`

Install it from [NuGet](https://www.nuget.org/packages/WTorricos.EitherUnion).

## Getting started

```csharp
using WTorricos.Either;
using System.Globalization;

IEither<int> result = new Ok<int>(42);

string text = result switch
{
    Ok<int> ok => ok.Value.ToString(CultureInfo.InvariantCulture),
    Failure failure => failure.GetDisplayMessage()
};
```

```csharp
Failure failure = new(
    ErrorCode: "NOT_FOUND",
    Message: "User not found",
    Level: Severity.Warning,
    Timestamp: DateTime.UtcNow,
    Details: [new Detail("USER_ID", "The requested user does not exist")]
);

Failure fromOffset = new(
    ErrorCode: "UNAVAILABLE",
    Message: "Service unavailable",
    Level: Severity.Error,
    Timestamp: DateTimeOffset.UtcNow,
    Details: []
);

IEither<int> result = failure;
```

## Composition

```csharp
IEither<string> email = GetUser(id)
    .Map(user => user.Email)
    .FlatMap(email => ValidateEmail(email));
```

```csharp
IEither<int> total =
    from first in GetFirstValue()
    from second in GetSecondValue(first)
    where second > 0
    select first + second;
```

## Custom failures

Custom failures are plain records that inherit from `Failure`.

```csharp
public record NotFoundFailure(string Resource, string? TraceId = null)
    : Failure(
        ErrorCode: "NOT_FOUND_404",
        Message: $"{Resource} not found",
        Level: Severity.Warning,
        Timestamp: DateTime.UtcNow,
        Details: [],
        TraceId: TraceId,
        StackTrace: Environment.StackTrace);
```

## Documentation

- [Design notes](./DESIGN.md)
- [Changelog](./CHANGELOG.md)

## Contributing

```powershell
dotnet build WTorricos.Either.slnx
dotnet test tests\WTorricos.Either.UnitTests\WTorricos.Either.UnitTests.csproj
./build.cmd CiBuildAndTest
```

The library targets `net11.0` preview tooling in this repository and packs as `WTorricos.EitherUnion`.
