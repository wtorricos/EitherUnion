using System.Globalization;
using Either.SampleApi.Infrastructure.Http;
using Either.SampleApi.Infrastructure.Persistence;
using Either.SampleApi.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using WTorricos.Either;

namespace Either.SampleApi.Features.Payments.Refund;

public static class RefundPaymentV2Endpoint
{
    public static IEndpointRouteBuilder MapRefundPaymentV2Endpoints(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapPost("/payments/refund/v2", HandleV2Async)
            .WithName("RefundPaymentV2");

        return endpoints;
    }

    private static async Task<IResult> HandleV2Async(
        RefundPaymentRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            IEither<RefundPaymentResponse> responseEither = await Validate(request)
                .OnFailure(failure => Console.WriteLine("Validation failed with {0}", failure.ErrorCode))
                .FlatMap(ValidateAmount)
                .Inspect(
                    onSuccess: _ => Console.WriteLine("Validation successful"),
                    onFailure: failure => Console.WriteLine("Validation failed with {0}", failure.ErrorCode))
                .FlatMapAsync(
                    validRequest => BuildRefundContextAsync(validRequest, dbContext, cancellationToken),
                    cancellationToken)
                .OnFailureAsync(failure => Console.WriteLine("Failed to create refund context {0}", failure.ErrorCode), cancellationToken)
                .MapAsync(
                    context => PersistRefundAsync(context, dbContext, cancellationToken),
                    cancellationToken)
                .TapAsync(ok => Console.WriteLine("Refund created successfully {0}", ok), cancellationToken)
                .MapAsync(
                    refund => new RefundPaymentResponse(refund.Id, refund.OrderId, refund.Amount, refund.Reason, refund.CreatedUtc),
                    cancellationToken)
                .InspectAsync(
                    onSuccess: ok => Console.WriteLine("Refund operation completed successfully {0}", ok),
                    onFailure: failure => Console.WriteLine("Refund failed {0}", failure),
                    cancellationToken: cancellationToken);

            return responseEither.ToOkResult();
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
    }

    private static IEither<RefundPaymentRequest> ValidateAmount(RefundPaymentRequest request) =>
        request.Amount > 0
            ? new Ok<RefundPaymentRequest>(request)
            : new Failure(
                ErrorCode: "VALIDATION_REFUND_AMOUNT",
                Message: "Refund amount must be greater than zero.",
                Level: Severity.Warning,
                Timestamp: DateTime.UtcNow,
                Details: [new Detail("AMOUNT", request.Amount.ToString("0.00", CultureInfo.InvariantCulture))]);

    private static IEither<RefundPaymentRequest> Validate(RefundPaymentRequest request) =>
        request.OrderId == Guid.Empty
            ? new Failure(
                ErrorCode: "VALIDATION_ORDER_ID",
                Message: "OrderId is required.",
                Level: Severity.Warning,
                Timestamp: DateTime.UtcNow,
                Details: [new Detail("ORDER_ID", "OrderId must be a non-empty GUID.")])
            : string.IsNullOrWhiteSpace(request.Reason)
            ? new Failure(
                ErrorCode: "VALIDATION_REASON",
                Message: "Refund reason is required.",
                Level: Severity.Warning,
                Timestamp: DateTime.UtcNow,
                Details: [new Detail("REASON", "Reason cannot be empty.")])
            : new Ok<RefundPaymentRequest>(request);

    private static async Task<IEither<RefundContext>> BuildRefundContextAsync(
        RefundPaymentRequest request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        OrderEntity? order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == request.OrderId, cancellationToken);

        Failure orderNotFoundFailure = new(
            ErrorCode: "ORDER_NOT_FOUND",
            Message: "Cannot refund an order that does not exist.",
            Level: Severity.Warning,
            Timestamp: DateTime.UtcNow,
            Details: [new Detail("ORDER_ID", request.OrderId.ToString("D"))]);

        IEither<OrderEntity> orderEither = IEitherExtensions.FromNullable(order, orderNotFoundFailure);

        return
            from existingOrder in orderEither
            from context in ValidateAmountAgainstOrder(request, existingOrder)
            select context;
    }

    private static IEither<RefundContext> ValidateAmountAgainstOrder(
        RefundPaymentRequest request,
        OrderEntity existingOrder) =>
        request.Amount > existingOrder.Amount
            ? new Failure(
                ErrorCode: "VALIDATION_REFUND_AMOUNT_EXCEEDS_ORDER",
                Message: "Refund amount cannot exceed the order amount.",
                Level: Severity.Warning,
                Timestamp: DateTime.UtcNow,
                Details:
                [
                    new Detail("ORDER_AMOUNT", existingOrder.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
                    new Detail("REFUND_AMOUNT", request.Amount.ToString("0.00", CultureInfo.InvariantCulture))
                ])
            : new Ok<RefundContext>(new RefundContext(existingOrder, request));

    private static async Task<RefundEntity> PersistRefundAsync(
        RefundContext context,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RefundEntity refund = new()
        {
            Id = Guid.NewGuid(),
            OrderId = context.Order.Id,
            Amount = context.Request.Amount,
            Reason = context.Request.Reason.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        _ = dbContext.Refunds.Add(refund);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return refund;
    }

    private sealed record RefundContext(OrderEntity Order, RefundPaymentRequest Request);
}
