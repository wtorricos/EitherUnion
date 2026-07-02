# WTorricos.Either

WTorricos.Either is a .NET library for expressive, type-safe error handling with `IEither<T>`, `Ok<T>`, and `Failure`.

Its goal is to provide a practical union for success-or-failure workflows in modern .NET: while union support now exists in the platform, there is still no native built-in union tailored to this common error-handling shape.

## Highlights

- `IEither<T>` union with `Ok<T>` and `Failure`
- Rich failure context: `ErrorCode`, `Severity`, `TraceId`, `StackTrace`, `InnerError`, `Metadata`
- Fluent helpers: `Map`, `FlatMap`, `Flatten`, `MapFailure`, `Match`, `Inspect`, `Tap`, `OnFailure`, `Filter`, `Void`
- Result extraction helpers: `GetValueOrThrow`
- Async helpers (Task and ValueTask): `MapAsync`, `FlatMapAsync`, `MatchAsync`, `ActionAsync`
- Extended async chaining helpers: `FlattenAsync`, `MapFailureAsync`, `InspectAsync`, `TapAsync`, `OnFailureAsync`, `FilterAsync`
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
    Details: [new Detail("USER_ID", "The requested user does not exist")],
    StackTrace: Environment.StackTrace
);

IEither<int> result = failure;
```

## Composition
LINQ syntax support for IEither<T>  
This syntax intentionally excludes Tasks, and ValueTasks, since async chaining should be handled via await.

```csharp
IEither<int> total =
    from first in GetFirstValue()
    from second in GetSecondValue(first)
    where second > 0
    select first + second;
```

Fluent syntax that supports Async flows

```csharp
IEither<RefundPaymentResponse> responseEither = await Validate(request)
    .FlatMap(ValidateAmount)
    .FlatMapAsync(
        validRequest => BuildRefundContextAsync(validRequest, dbContext, cancellationToken),
        cancellationToken)
    .MapAsync(
        context => PersistRefundAsync(context, dbContext, cancellationToken),
        cancellationToken)
    .MapAsync(
        refund => new RefundPaymentResponse(refund.Id, refund.OrderId, refund.Amount, refund.Reason, refund.CreatedUtc),
        cancellationToken)
    .InspectAsync(
        onSuccess: ok => Console.WriteLine("Refund operation completed successfully {0}", ok),
        onFailure: failure => Console.WriteLine("Refund failed {0}", failure),
        cancellationToken: cancellationToken);;
```

ValueTask fluent syntax.
Chaining is intentionally limited because the recommended practice is to explicitly await ValueTask instances.

```csharp
IEither<RefundContext> refundCcontextEither = await requestEither.FlatMapAsync(
    validRequest => BuildRefundContextAsync(validRequest, dbContext, cancellationToken),
    cancellationToken);

IEither<RefundEntity> result = await refundCcontextEither.MapAsync(
    context => PersistRefundAsync(context, dbContext, cancellationToken),
    cancellationToken);
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
- [Sample API](./samples/Either.SampleApi/Readme.md)

## Sample API (vertical slices)

`samples/Either.SampleApi` demonstrates a medium-complexity minimal API with practical `IEither<T>` flows:

- `POST /orders` uses `FlatMap` + `FlatMapAsync`
- `GET /orders/{id}` uses `FromNullable` + `MapFailure`
- `POST /payments/refund` uses `Filter` + `MapAsync` and returns HTTP `499` on cancellation
- `POST /payments/refund/v2` shows the Task-based async chaining flow and returns HTTP `499` on cancellation

## Contributing

```powershell
dotnet build WTorricos.Either.slnx
dotnet test tests\WTorricos.Either.UnitTests\WTorricos.Either.UnitTests.csproj
./build.cmd CiBuildAndTest
```

The library targets `net11.0` preview tooling in this repository and packs as `WTorricos.EitherUnion`.
