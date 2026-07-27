using Contoso.InventoryFunctions.Contracts;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Services;

public sealed class InMemoryInventoryService : IInventoryService
{
    private const int AvailableInventory = 20;

    private readonly ILogger<InMemoryInventoryService> _logger;

    public InMemoryInventoryService(
        ILogger<InMemoryInventoryService> logger)
    {
        _logger = logger;
    }

    public Task<ReserveInventoryResponse> ReserveAsync(
        ReserveInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Quantity > AvailableInventory)
        {
            _logger.LogWarning(
                "Insufficient inventory for order {OrderId}. " +
                "Product: {ProductId}; requested: {Requested}; available: {Available}.",
                request.OrderId,
                request.ProductId,
                request.Quantity,
                AvailableInventory);

            return Task.FromResult(new ReserveInventoryResponse(
                Success: false,
                ReservationId: null,
                OrderId: request.OrderId,
                ProductId: request.ProductId,
                QuantityReserved: 0,
                RemainingInventory: AvailableInventory,
                ErrorCode: "OUT_OF_STOCK",
                Message: "There is not enough inventory to complete the reservation."));
        }

        var reservationId = $"RES-{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Created inventory reservation {ReservationId} for order {OrderId}.",
            reservationId,
            request.OrderId);

        return Task.FromResult(new ReserveInventoryResponse(
            Success: true,
            ReservationId: reservationId,
            OrderId: request.OrderId,
            ProductId: request.ProductId,
            QuantityReserved: request.Quantity,
            RemainingInventory: AvailableInventory - request.Quantity));
    }
}