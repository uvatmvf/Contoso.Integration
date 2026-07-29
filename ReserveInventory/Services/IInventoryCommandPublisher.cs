using Contoso.InventoryFunctions.Contracts;

namespace Contoso.InventoryFunctions.Services
{
    public interface IInventoryCommandPublisher
    {
        Task PublishReserveInventoryAsync(
            ReserveInventoryRequest request,
            CancellationToken cancellationToken);
    }
}
