using System.Collections.Concurrent;
using Contoso.InventoryFunctions.Contracts;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Services;

public sealed class InMemoryInventoryService : IInventoryService
{
    private readonly ILogger<InMemoryInventoryService> _logger;
    private readonly object _inventoryLock = new();

    private readonly ConcurrentDictionary<string, ReservationRecord>
        _reservations = new();

    private int _availableInventory = 20;

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

        lock (_inventoryLock)
        {
            if (request.Quantity > _availableInventory)
            {
                _logger.LogWarning(
                    "Inventory reservation rejected. OrderId: {OrderId}, ProductId: {ProductId}, Requested: {Quantity}, Available: {Available}",
                    request.OrderId,
                    request.ProductId,
                    request.Quantity,
                    _availableInventory);

                return Task.FromResult(
                    new ReserveInventoryResponse(
                        Success: false,
                        ReservationId: null,
                        OrderId: request.OrderId,
                        ProductId: request.ProductId,
                        QuantityReserved: 0,
                        RemainingInventory: _availableInventory,
                        ErrorCode: "OUT_OF_STOCK",
                        Message: "There is not enough inventory to complete the reservation."));
            }

            var reservationId = $"RES-{Guid.NewGuid():N}";

            _availableInventory -= request.Quantity;

            _reservations[reservationId] = new ReservationRecord(
                ReservationId: reservationId,
                OrderId: request.OrderId,
                ProductId: request.ProductId,
                Quantity: request.Quantity,
                Released: false);

            _logger.LogInformation(
                "Inventory reserved. ReservationId: {ReservationId}, OrderId: {OrderId}, ProductId: {ProductId}, Quantity: {Quantity}, Remaining: {Remaining}",
                reservationId,
                request.OrderId,
                request.ProductId,
                request.Quantity,
                _availableInventory);

            return Task.FromResult(
                new ReserveInventoryResponse(
                    Success: true,
                    ReservationId: reservationId,
                    OrderId: request.OrderId,
                    ProductId: request.ProductId,
                    QuantityReserved: request.Quantity,
                    RemainingInventory: _availableInventory,
                    ErrorCode: null,
                    Message: null));
        }
    }

    public Task<ReleaseInventoryResponse> ReleaseAsync(
        ReleaseInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_inventoryLock)
        {
            if (!_reservations.TryGetValue(
                    request.ReservationId,
                    out var reservation))
            {
                return Task.FromResult(
                    new ReleaseInventoryResponse(
                        Success: false,
                        OrderId: request.OrderId,
                        ReservationId: request.ReservationId,
                        ErrorCode: "RESERVATION_NOT_FOUND",
                        Message: "The inventory reservation was not found."));
            }

            if (!string.Equals(
                    reservation.OrderId,
                    request.OrderId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(
                    new ReleaseInventoryResponse(
                        Success: false,
                        OrderId: request.OrderId,
                        ReservationId: request.ReservationId,
                        ErrorCode: "ORDER_MISMATCH",
                        Message: "The reservation does not belong to the supplied order."));
            }

            if (reservation.Released)
            {
                _logger.LogInformation(
                    "Inventory reservation was already released. ReservationId: {ReservationId}",
                    request.ReservationId);

                return Task.FromResult(
                    new ReleaseInventoryResponse(
                        Success: true,
                        OrderId: request.OrderId,
                        ReservationId: request.ReservationId,
                        ErrorCode: null,
                        Message: "The inventory reservation was already released."));
            }

            _availableInventory += reservation.Quantity;

            _reservations[request.ReservationId] =
                reservation with
                {
                    Released = true
                };

            _logger.LogInformation(
                "Inventory released. ReservationId: {ReservationId}, OrderId: {OrderId}, Quantity: {Quantity}, Available: {Available}",
                request.ReservationId,
                request.OrderId,
                reservation.Quantity,
                _availableInventory);

            return Task.FromResult(
                new ReleaseInventoryResponse(
                    Success: true,
                    OrderId: request.OrderId,
                    ReservationId: request.ReservationId,
                    ErrorCode: null,
                    Message: null));
        }
    }

    private sealed record ReservationRecord(
        string ReservationId,
        string OrderId,
        string ProductId,
        int Quantity,
        bool Released);
}