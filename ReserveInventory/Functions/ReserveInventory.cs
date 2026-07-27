using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

    // Back-compat constructor used by unit tests that only provide a logger.
    // Creates a default in-memory implementation so tests can instantiate the
    // function without wiring DI.
    public ReserveInventory(ILogger<ReserveInventory> logger)
    {
        _inventoryService = new InMemoryInventoryService(new NullLogger<InMemoryInventoryService>());
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

            return new BadRequestObjectResult(new ErrorResponse(
                ErrorCode: "INVALID_REQUEST",
                Message: "The request body must contain valid JSON."));
        }

        if (reservationRequest is null)
        {
            return new BadRequestObjectResult(new ErrorResponse(
                ErrorCode: "MISSING_BODY",
                Message: "A request body is required."));
        }

        if (string.IsNullOrWhiteSpace(reservationRequest.OrderId) ||
            string.IsNullOrWhiteSpace(reservationRequest.ProductId) ||
            reservationRequest.Quantity <= 0)
        {
            return new BadRequestObjectResult(new ErrorResponse(
                ErrorCode: "INVALID_REQUEST",
                Message: "OrderId, ProductId, and a positive Quantity are required."));
        }

        var result = await _inventoryService.ReserveAsync(
            reservationRequest,
            cancellationToken);

        return result.Success
            ? new OkObjectResult(result)
            : new ConflictObjectResult(new ErrorResponse(
                ErrorCode: result.ErrorCode ?? "OUT_OF_STOCK",
                Message: result.Message ?? "There is not enough inventory to complete the reservation."));
    }
}