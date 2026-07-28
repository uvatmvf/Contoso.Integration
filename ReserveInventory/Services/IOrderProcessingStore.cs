using Contoso.InventoryFunctions.Contracts;
namespace Contoso.InventoryFunctions.Services;

public interface IOrderProcessingStore
{
    Task<OrderProcessingEntity> GetOrCreateAsync(
        string orderId,
        CancellationToken cancellationToken);

    Task MarkInventoryReservedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken);

    Task MarkPaymentAuthorizedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken);

    Task MarkPaymentFailedAsync(
        OrderProcessingEntity entity,
        string error,
        CancellationToken cancellationToken);
}