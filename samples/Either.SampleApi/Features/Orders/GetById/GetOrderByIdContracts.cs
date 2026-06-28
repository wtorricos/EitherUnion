namespace Either.SampleApi.Features.Orders.GetById;

public sealed record GetOrderByIdResponse(Guid Id, string CustomerName, decimal Amount, string Currency, DateTime CreatedUtc);
