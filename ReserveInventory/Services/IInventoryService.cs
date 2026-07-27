using Contoso.InventoryFunctions.Contracts;

namespace Contoso.InventoryFunctions.Services;

public interface IInventoryService
{
    Task<ReserveInventoryResponse> ReserveAsync(
        ReserveInventoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ReleaseInventoryResponse> ReleaseAsync(
        ReleaseInventoryRequest request,
        CancellationToken cancellationToken = default);
}