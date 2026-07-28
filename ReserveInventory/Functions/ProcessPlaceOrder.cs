using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Contoso.InventoryFunctions.Functions;

public sealed class ProcessPlaceOrder
{
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessPlaceOrder> _logger;

    public ProcessPlaceOrder(
        IInventoryService inventoryService,
        IPaymentService paymentService,
        ILogger<ProcessPlaceOrder> logger)
    {
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _logger = logger;
    }

    [Function(nameof(ProcessPlaceOrder))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            "place-order",
            Connection = "ServiceBusConnection")]
        string messageBody,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received place-order message: {MessageBody}",
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
                "The place-order message could not be deserialized.");
        }

        Validate(command);

        using var logScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["OrderId"] = command.OrderId,
                ["CustomerId"] = command.CustomerId,
                ["ProductId"] = command.ProductId
            });

        _logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}",
            command.OrderId,
            command.CustomerId);

        await _inventoryService.ReserveAsync(
            new ReserveInventoryRequest(
                command.OrderId,
                command.ProductId,
                command.Quantity),
            cancellationToken);

        _logger.LogInformation(
            "Inventory reserved for order {OrderId}",
            command.OrderId);

        await _paymentService.AuthorizeAsync(
            new AuthorizePaymentRequest(
                command.OrderId,
                command.PaymentMethodId,
                command.Amount),
            cancellationToken);

        _logger.LogInformation(
            "Payment authorized for order {OrderId}. Amount: {Amount}",
            command.OrderId,
            command.Amount);

        _logger.LogInformation(
            "Order {OrderId} processed successfully",
            command.OrderId);
    }

    private static void Validate(PlaceOrderCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.OrderId))
        {
            throw new InvalidOperationException(
                "OrderId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.CustomerId))
        {
            throw new InvalidOperationException(
                "CustomerId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ProductId))
        {
            throw new InvalidOperationException(
                "ProductId is required.");
        }

        if (command.Quantity <= 0)
        {
            throw new InvalidOperationException(
                "Quantity must be greater than zero.");
        }

        if (command.Amount < 0)
        {
            throw new InvalidOperationException(
                "Amount cannot be negative.");
        }
    }
}