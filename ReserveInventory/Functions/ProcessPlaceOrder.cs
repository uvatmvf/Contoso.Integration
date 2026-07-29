using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Contoso.InventoryFunctions.Functions;

public sealed class ProcessPlaceOrder
{
    private readonly IInventoryCommandPublisher _inventoryCommandPublisher;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessPlaceOrder> _logger;
    private readonly IOrderProcessingStore _orderStore;

    public ProcessPlaceOrder(
        IInventoryCommandPublisher inventoryService,
        IPaymentService paymentService,
        ILogger<ProcessPlaceOrder> logger,
        IOrderProcessingStore orderStore)
    {
        _inventoryCommandPublisher = inventoryService;
        _paymentService = paymentService;
        _logger = logger;
        _orderStore = orderStore;
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

        var state = await _orderStore.GetOrCreateAsync(
            command.OrderId,
            cancellationToken);

        if (state.OrderStatus == "Completed")
        {
            _logger.LogInformation(
                "Order {OrderId} was already completed. Skipping.",
                command.OrderId);

            return;
        }

        if (state.InventoryStatus != "Completed")
        {
            var operationId = $"{command.OrderId}:reserve-inventory";
            await _inventoryCommandPublisher.PublishReserveInventoryAsync(
                new ReserveInventoryRequest(
                    command.OrderId,
                    operationId,
                    command.ProductId,
                    command.Quantity),
                cancellationToken);

            _logger.LogInformation(
                "Reserve inventory command published for order {OrderId}",
                command.OrderId);
        }
        else
        {
            _logger.LogInformation(
                "Inventory was already reserved for order {OrderId}. Skipping.",
                command.OrderId);
        }

        try
        {
            if (state.PaymentStatus != "Completed")
            {
                var paymentResult = await _paymentService.AuthorizeAsync(
                        new AuthorizePaymentRequest(
                            command.OrderId,
                            command.PaymentMethodId,
                            command.Amount),
                        cancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} processed successfully",
                    command.OrderId);

                if (!paymentResult.Success)
                {
                    throw new InvalidOperationException(
                        $"Payment failed: {paymentResult.ErrorCode}");
                }

                await _orderStore.MarkPaymentAuthorizedAsync(
                    state,
                    cancellationToken);

                _logger.LogInformation(
                    "Payment authorized for order {OrderId}. Amount: {Amount}",
                    command.OrderId,
                    command.Amount);
            }
        }
        catch (Exception exception)
        {
            await _orderStore.MarkPaymentFailedAsync(
                state,
                exception.Message,
                cancellationToken);

            throw;
        }
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