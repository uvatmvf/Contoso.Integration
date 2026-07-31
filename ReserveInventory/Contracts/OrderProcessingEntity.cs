using Azure;
using Azure.Data.Tables;

namespace Contoso.InventoryFunctions.Contracts;

public sealed class OrderProcessingEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Order";

    public string RowKey { get; set; } = default!;

    public string InventoryStatus { get; set; } = "NotStarted";

    public string PaymentStatus { get; set; } = "NotStarted";

    public string OrderStatus { get; set; } = "Pending";

    public string? LastError { get; set; }

    public string PaymentMethodId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public DateTimeOffset? InventoryReservedAt { get; set; }

    public DateTimeOffset? PaymentAuthorizedAt { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
    public DateTimeOffset? OrderCompletedAt { get; internal set; }

}