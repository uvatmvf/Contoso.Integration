using Azure;
using Azure.Data.Tables;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;

public sealed class TableOrderProcessingStore : IOrderProcessingStore
{
    private const string PartitionKey = "Order";
    //private const string OrderCompleted = "Completed";
    //private const string OrderProcessing = "Processing";
    //private const string InventoryReserved = "Reserved";
    private readonly TableClient _tableClient;

    public TableOrderProcessingStore(TableClient tableClient)
    {
        _tableClient = tableClient;
    }

    public async Task<GetOrCreateOrderResult> GetOrCreateAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await GetAsync(
        command.OrderId,
        cancellationToken);

        if (existing is not null)
        {
            return new GetOrCreateOrderResult(existing, false);
        }

        var entity = new OrderProcessingEntity
        {
            PartitionKey = PartitionKey,
            RowKey = command.OrderId,
            ETag = ETag.All,
            InventoryStatus = InventoryStatuses.NotStarted,
            PaymentStatus = PaymentStatuses.NotStarted,
            OrderStatus = OrderStatuses.Pending,
            PaymentMethodId = command.PaymentMethodId,
            Amount = command.Amount,
            Currency = command.Currency
        };

        await _tableClient.AddEntityAsync(
            entity,
            cancellationToken);

        return new GetOrCreateOrderResult(entity, true);
    }

    public async Task<OrderProcessingEntity?> GetAsync(
    string orderId,
    CancellationToken cancellationToken)
    {
        var response =
            await _tableClient.GetEntityIfExistsAsync<OrderProcessingEntity>(
                partitionKey: PartitionKey,
                rowKey: orderId,
                cancellationToken: cancellationToken);

        return response.HasValue
            ? response.Value
            : null;
    }

    public async Task MarkInventoryReservedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken)
    {
        entity.InventoryStatus = InventoryStatuses.Reserved;
        entity.InventoryReservedAt = DateTimeOffset.UtcNow;

        await UpdateAsync(entity, cancellationToken);
    }

    public async Task MarkOrderCompletedAsync(
    OrderProcessingEntity entity,
    CancellationToken cancellationToken)
    {
        entity.OrderStatus = OrderStatuses.Completed;
        entity.LastError = null;
        entity.OrderCompletedAt = DateTimeOffset.UtcNow;
        await UpdateAsync(entity, cancellationToken);
    }

    public async Task MarkPaymentAuthorizedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken)
    {
        entity.PaymentStatus = PaymentStatuses.Completed;
        entity.PaymentAuthorizedAt = DateTimeOffset.UtcNow;
        entity.OrderStatus = OrderStatuses.Processing;
        entity.LastError = null;

        await UpdateAsync(entity, cancellationToken);
    }

    public async Task MarkPaymentFailedAsync(
        OrderProcessingEntity entity,
        string error,
        CancellationToken cancellationToken)
    {       
        entity.PaymentStatus = PaymentStatuses.Failed;
        entity.PaymentAuthorizedAt = DateTimeOffset.UtcNow;
        entity.OrderStatus = OrderStatuses.Processing;
        entity.LastError = error;

        await UpdateAsync(entity, cancellationToken);
    }

    private async Task UpdateAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken)
    {
        var response = await _tableClient.UpdateEntityAsync(
            entity,
            entity.ETag,
            TableUpdateMode.Merge,
            cancellationToken);

        entity.ETag = response.Headers.ETag ?? entity.ETag;
    }

}