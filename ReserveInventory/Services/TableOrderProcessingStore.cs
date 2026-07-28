using Azure;
using Azure.Data.Tables;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;

public sealed class TableOrderProcessingStore : IOrderProcessingStore
{
    private const string PartitionKey = "Order";

    private readonly TableClient _tableClient;

    public TableOrderProcessingStore(TableClient tableClient)
    {
        _tableClient = tableClient;
    }

    public async Task<OrderProcessingEntity> GetOrCreateAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await _tableClient.GetEntityAsync<OrderProcessingEntity>(
                    PartitionKey,
                    orderId,
                    cancellationToken: cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException exception)
            when (exception.Status == 404)
        {
            var entity = new OrderProcessingEntity
            {
                PartitionKey = PartitionKey,
                RowKey = orderId,
                InventoryStatus = "NotStarted",
                PaymentStatus = "NotStarted",
                OrderStatus = "Processing"
            };

            try
            {
                await _tableClient.AddEntityAsync(
                    entity,
                    cancellationToken);

                return entity;
            }
            catch (RequestFailedException rfException)
                when (rfException.Status == 409)
            {
                // Another Function instance created it first.
                var response =
                    await _tableClient.GetEntityAsync<OrderProcessingEntity>(
                        PartitionKey,
                        orderId,
                        cancellationToken: cancellationToken);

                return response.Value;
            }
        }
    }

    public async Task MarkInventoryReservedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken)
    {
        entity.InventoryStatus = "Completed";
        entity.InventoryReservedAt = DateTimeOffset.UtcNow;

        await UpdateAsync(entity, cancellationToken);
    }

    public async Task MarkPaymentAuthorizedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken)
    {
        entity.PaymentStatus = "Completed";
        entity.PaymentAuthorizedAt = DateTimeOffset.UtcNow;
        entity.OrderStatus = "Completed";
        entity.LastError = null;

        await UpdateAsync(entity, cancellationToken);
    }

    public async Task MarkPaymentFailedAsync(
        OrderProcessingEntity entity,
        string error,
        CancellationToken cancellationToken)
    {
        entity.PaymentStatus = "Failed";
        entity.OrderStatus = "Processing";
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
            TableUpdateMode.Replace,
            cancellationToken);

        entity.ETag = response.Headers.ETag ?? entity.ETag;
    }
}