using Either.SampleApi.Infrastructure.Http;
using Either.SampleApi.Infrastructure.Persistence;
using Either.SampleApi.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using WTorricos.Either;

namespace Either.SampleApi.Features.Orders.GetById;

public static class GetOrderByIdEndpoint
{
    public static IEndpointRouteBuilder MapGetOrderByIdEndpoints(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapGet("/orders/{id:guid}", HandleAsync)
            .WithName("GetOrderById");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(Guid id, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        OrderEntity? order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        Failure missingOrderFailure = new(
            ErrorCode: "ORDER_NOT_FOUND",
            Message: "The requested order does not exist.",
            Level: Severity.Warning,
            Timestamp: DateTime.UtcNow,
            Details: [new Detail("ORDER_ID", id.ToString("D"))]);

        IEither<OrderEntity> result = IEitherExtensions
            .FromNullable(order, missingOrderFailure)
            .MapFailure(failure => failure with
            {
                Details =
                [
                    .. failure.Details,
                    new Detail("ENDPOINT", "GET /orders/{id}")
                ]
            });

        return result.Match(
            onSuccess: existingOrder => Results.Ok(
                new GetOrderByIdResponse(
                    existingOrder.Id,
                    existingOrder.CustomerName,
                    existingOrder.Amount,
                    existingOrder.Currency,
                    existingOrder.CreatedUtc)),
            onFailure: failure => failure.ToProblemResult());
    }
}
