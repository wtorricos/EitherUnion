namespace Either.SampleApi.Infrastructure.Persistence.Entities;

public sealed class RefundEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}
