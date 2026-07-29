using Contoso.InventoryFunctions.Contracts;

namespace Contoso.InventoryFunctions.Services;

public interface IOrderEventPublisher
{
    Task PublishInventoryReservedAsync(
        InventoryReservedEvent inventoryReserved,
        CancellationToken cancellationToken);
}
