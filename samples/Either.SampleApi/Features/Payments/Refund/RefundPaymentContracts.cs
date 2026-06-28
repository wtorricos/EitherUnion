namespace Either.SampleApi.Features.Payments.Refund;

public sealed record RefundPaymentRequest(Guid OrderId, decimal Amount, string Reason);

public sealed record RefundPaymentResponse(Guid RefundId, Guid OrderId, decimal Amount, string Reason, DateTime RefundedUtc);
