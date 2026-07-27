using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class ReserveInventory
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReserveInventory> _logger;

    public ReserveInventory(
        IInventoryService inventoryService,
        ILogger<ReserveInventory> logger)
    {
        _inventoryService = inventoryService;
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
                "Inventory reservation request contained invalid JSON.");

            return new BadRequestObjectResult(new
            {
                errorCode = "INVALID_JSON",
                message = "The request body must contain valid JSON."
            });
        }

        if (reservationRequest is null)
        {
            return new BadRequestObjectResult(new
            {
                errorCode = "MISSING_BODY",
                message = "A request body is required."
            });
        }

        if (string.IsNullOrWhiteSpace(reservationRequest.OrderId) ||
            string.IsNullOrWhiteSpace(reservationRequest.ProductId) ||
            reservationRequest.Quantity <= 0)
        {
            return new BadRequestObjectResult(new
            {
                errorCode = "INVALID_REQUEST",
                message = "OrderId, ProductId, and a positive Quantity are required."
            });
        }

        var result = await _inventoryService.ReserveAsync(
            reservationRequest,
            cancellationToken);

        return result.Success
            ? new OkObjectResult(result)
            : new ConflictObjectResult(result);
    }
}