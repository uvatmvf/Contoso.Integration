using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions;

public sealed class ReserveInventory
{
    private const int AvailableInventory = 20;

    private readonly ILogger<ReserveInventory> _logger;

    public ReserveInventory(ILogger<ReserveInventory> logger)
    {
        _logger = logger;
    }

    [Function("ReserveInventory")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "post",
            Route = "inventory/reservations")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        ReserveInventoryRequest? reservationRequest;

        try
        {
            reservationRequest =
                await request.ReadFromJsonAsync<ReserveInventoryRequest>(
                    cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "The inventory reservation request contained invalid JSON.");

            return new BadRequestObjectResult(new ErrorResponse(
                "INVALID_REQUEST",
                "The request body must contain valid JSON."));
        }

        if (reservationRequest is null)
        {
            return new BadRequestObjectResult(new ErrorResponse(
                "INVALID_REQUEST",
                "A request body is required."));
        }

        if (string.IsNullOrWhiteSpace(reservationRequest.OrderId) ||
            string.IsNullOrWhiteSpace(reservationRequest.ProductId) ||
            reservationRequest.Quantity <= 0)
        {
            return new BadRequestObjectResult(new ErrorResponse(
                "INVALID_REQUEST",
                "OrderId, ProductId, and a positive Quantity are required."));
        }

        _logger.LogInformation(
            "Attempting to reserve {Quantity} units of product {ProductId} " +
            "for order {OrderId}.",
            reservationRequest.Quantity,
            reservationRequest.ProductId,
            reservationRequest.OrderId);

        if (reservationRequest.Quantity > AvailableInventory)
        {
            _logger.LogWarning(
                "Inventory reservation rejected for order {OrderId}. " +
                "Requested {RequestedQuantity}; available {AvailableQuantity}.",
                reservationRequest.OrderId,
                reservationRequest.Quantity,
                AvailableInventory);

            return new ConflictObjectResult(new ErrorResponse(
                "OUT_OF_STOCK",
                "There is not enough inventory to complete the reservation."));
        }

        var response = new ReserveInventoryResponse(
            Success: true,
            ReservationId: $"RES-{Guid.NewGuid():N}",
            OrderId: reservationRequest.OrderId,
            ProductId: reservationRequest.ProductId,
            QuantityReserved: reservationRequest.Quantity,
            RemainingInventory: AvailableInventory - reservationRequest.Quantity);

        _logger.LogInformation(
            "Inventory reservation {ReservationId} created for order {OrderId}.",
            response.ReservationId,
            response.OrderId);

        return new OkObjectResult(response);
    }
}

public sealed record ReserveInventoryRequest(
    string OrderId,
    string ProductId,
    int Quantity);

public sealed record ReserveInventoryResponse(
    bool Success,
    string ReservationId,
    string OrderId,
    string ProductId,
    int QuantityReserved,
    int RemainingInventory);

public sealed record ErrorResponse(
    string ErrorCode,
    string Message);