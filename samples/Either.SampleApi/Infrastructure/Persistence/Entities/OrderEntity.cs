namespace Either.SampleApi.Infrastructure.Persistence.Entities;

public sealed class OrderEntity
{
    public Guid Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}
