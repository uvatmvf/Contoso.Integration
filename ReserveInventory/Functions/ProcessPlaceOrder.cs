using System.Text.Json;
using Contoso.InventoryFunctions.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class ProcessPlaceOrder
{
    private readonly ILogger<ProcessPlaceOrder> _logger;

    public ProcessPlaceOrder(
        ILogger<ProcessPlaceOrder> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessPlaceOrder))]
    public void Run(
        [Microsoft.Azure.Functions.Worker.ServiceBusTrigger(
            "place-order",
            Connection = "ServiceBusConnection")]
        string messageBody)
    {
        _logger.LogInformation(
            "Received PlaceOrder message: {MessageBody}",
            messageBody);

        var command = JsonSerializer.Deserialize<PlaceOrderCommand>(
            messageBody,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (command is null)
        {
            throw new InvalidOperationException(
                "The PlaceOrder message could not be deserialized.");
        }

        _logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}. Product: {ProductId}, Quantity: {Quantity}, Amount: {Amount}",
            command.OrderId,
            command.CustomerId,
            command.ProductId,
            command.Quantity,
            command.Amount);
    }
}