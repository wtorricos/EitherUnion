using Either.SampleApi.Infrastructure.Http;
using Either.SampleApi.Infrastructure.Persistence;
using Either.SampleApi.Infrastructure.Persistence.Entities;
using WTorricos.Either;

namespace Either.SampleApi.Features.Orders.Create;

public static class CreateOrderEndpoint
{
    private static readonly HashSet<string> supportedCurrencies = ["USD", "EUR"];

    public static IEndpointRouteBuilder MapCreateOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapPost("/orders", HandleAsync)
            .WithName("CreateOrder");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        CreateOrderRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        IEither<CreateOrderRequest> requestEither = Validate(request)
            .FlatMap(EnsureCurrencyIsSupported);

        IEither<OrderEntity> result = await requestEither.FlatMapAsync(
            validRequest => CreateOrderAsync(validRequest, dbContext, cancellationToken),
            cancellationToken);

        return result switch
        {
            Ok<OrderEntity> order => Results.Created(
                $"/orders/{order.Value.Id:D}",
                new CreateOrderResponse(order.Value.Id, order.Value.CustomerName, order.Value.Amount, order.Value.Currency, order.Value.CreatedUtc)),
            Failure failure => failure.ToProblemResult()
        };
    }

    private static IEither<CreateOrderRequest> Validate(CreateOrderRequest request) =>
        string.IsNullOrWhiteSpace(request.CustomerName)
            ? BuildValidationFailure("CUSTOMER_NAME", "Customer name is required.")
            : request.Amount <= 0
            ? BuildValidationFailure("AMOUNT", "Order amount must be greater than zero.")
            : string.IsNullOrWhiteSpace(request.Currency)
            ? BuildValidationFailure("CURRENCY", "Currency is required.")
            : new Ok<CreateOrderRequest>(request);

    private static IEither<CreateOrderRequest> EnsureCurrencyIsSupported(CreateOrderRequest request) => supportedCurrencies.Contains(request.Currency)
            ? new Ok<CreateOrderRequest>(request)
            : new Failure(
                ErrorCode: "VALIDATION_UNSUPPORTED_CURRENCY",
                Message: "The provided currency is not supported for this sample.",
                Level: Severity.Warning,
                Timestamp: DateTime.UtcNow,
                Details: [new Detail("CURRENCY", request.Currency)]);

    private static async ValueTask<IEither<OrderEntity>> CreateOrderAsync(
        CreateOrderRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        OrderEntity order = new()
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName.Trim(),
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            CreatedUtc = DateTime.UtcNow
        };

        _ = dbContext.Orders.Add(order);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new Ok<OrderEntity>(order);
    }

    private static Failure BuildValidationFailure(string code, string description) =>
        new(
            ErrorCode: $"VALIDATION_{code}",
            Message: description,
            Level: Severity.Warning,
            Timestamp: DateTime.UtcNow,
            Details: [new Detail(code, description)]);
}
