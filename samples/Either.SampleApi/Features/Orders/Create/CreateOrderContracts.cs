namespace Either.SampleApi.Features.Orders.Create;

public sealed record CreateOrderRequest(string CustomerName, decimal Amount, string Currency);

public sealed record CreateOrderResponse(Guid Id, string CustomerName, decimal Amount, string Currency, DateTime CreatedUtc);
