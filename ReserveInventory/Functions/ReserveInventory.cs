using Azure.Messaging.ServiceBus;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

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
    public async Task Run(
        [ServiceBusTrigger(
            "reserve-inventory",
            Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        ReserveInventoryRequest? reservationRequest;

        try
        {
            reservationRequest = JsonSerializer.Deserialize<ReserveInventoryRequest>(message.Body);

            var result = await _inventoryService.ReserveAsync(
                reservationRequest,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Inventory reservation request contained invalid JSON.");           
        }



    }
}