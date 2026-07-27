using System.Net;
using System.Text.Json;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class AuthorizePayment
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<AuthorizePayment> _logger;

    public AuthorizePayment(
        IPaymentService paymentService,
        ILogger<AuthorizePayment> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [Function(nameof(AuthorizePayment))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "post",
            Route = "payments/authorizations")]
        HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var paymentRequest =
                await JsonSerializer.DeserializeAsync<AuthorizePaymentRequest>(
                    request.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    },
                    cancellationToken);

            if (paymentRequest is null)
            {
                return await CreateErrorResponseAsync(
                    request,
                    HttpStatusCode.BadRequest,
                    "INVALID_REQUEST",
                    "The request body is required.",
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(paymentRequest.OrderId))
            {
                return await CreateErrorResponseAsync(
                    request,
                    HttpStatusCode.BadRequest,
                    "INVALID_ORDER_ID",
                    "OrderId is required.",
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(paymentRequest.PaymentMethodId))
            {
                return await CreateErrorResponseAsync(
                    request,
                    HttpStatusCode.BadRequest,
                    "INVALID_PAYMENT_METHOD",
                    "PaymentMethodId is required.",
                    cancellationToken);
            }

            if (paymentRequest.Amount <= 0)
            {
                return await CreateErrorResponseAsync(
                    request,
                    HttpStatusCode.BadRequest,
                    "INVALID_AMOUNT",
                    "Amount must be greater than zero.",
                    cancellationToken);
            }

            var result = await _paymentService.AuthorizeAsync(
                paymentRequest,
                cancellationToken);

            var response = request.CreateResponse(
                result.Success
                    ? HttpStatusCode.OK
                    : HttpStatusCode.Conflict);

            await response.WriteAsJsonAsync(
                result,
                cancellationToken);

            return response;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid JSON received by AuthorizePayment.");

            return await CreateErrorResponseAsync(
                request,
                HttpStatusCode.BadRequest,
                "INVALID_JSON",
                "The request body contains invalid JSON.",
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected error while authorizing payment.");

            return await CreateErrorResponseAsync(
                request,
                HttpStatusCode.InternalServerError,
                "PAYMENT_SERVICE_ERROR",
                "An unexpected error occurred while authorizing payment.",
                cancellationToken);
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string errorCode,
        string message,
        CancellationToken cancellationToken)
    {
        var response = request.CreateResponse(statusCode);

        await response.WriteAsJsonAsync(
            new
            {
                errorCode,
                message
            },
            cancellationToken);

        return response;
    }
}