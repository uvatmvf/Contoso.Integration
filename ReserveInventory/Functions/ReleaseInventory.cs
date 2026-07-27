using System.Net;
using System.Text.Json;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class ReleaseInventory
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ReleaseInventory> _logger;

    public ReleaseInventory(
        IInventoryService inventoryService,
        ILogger<ReleaseInventory> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    [Function(nameof(ReleaseInventory))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "post",
            Route = "inventory/releases")]
        HttpRequestData request,
        CancellationToken cancellationToken)
    {
        try
        {
            var releaseRequest =
                await JsonSerializer.DeserializeAsync<ReleaseInventoryRequest>(
                    request.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    },
                    cancellationToken);

            if (releaseRequest is null)
            {
                return await CreateErrorResponseAsync(
                    request,
                    HttpStatusCode.BadRequest,
                    "INVALID_REQUEST",
                    "The request body is required.",
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(releaseRequest.OrderId))
            {
                return await CreateErrorResponseAsync(
                    request,
                    HttpStatusCode.BadRequest,
                    "INVALID_ORDER_ID",
                    "OrderId is required.",
                    cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(releaseRequest.ReservationId))
            {
                return await CreateErrorResponseAsync(
                    request,
                    HttpStatusCode.BadRequest,
                    "INVALID_RESERVATION_ID",
                    "ReservationId is required.",
                    cancellationToken);
            }

            var result = await _inventoryService.ReleaseAsync(
                releaseRequest,
                cancellationToken);

            var response = request.CreateResponse(
                result.Success
                    ? HttpStatusCode.OK
                    : HttpStatusCode.NotFound);

            await WriteJsonAsync(response, result, cancellationToken);

            return response;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid JSON received by ReleaseInventory.");

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
                "Unexpected error while releasing inventory.");

            return await CreateErrorResponseAsync(
                request,
                HttpStatusCode.InternalServerError,
                "INVENTORY_RELEASE_ERROR",
                "An unexpected error occurred while releasing inventory.",
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
        await WriteJsonAsync(response, new { errorCode, message }, cancellationToken);
        return response;
    }

    private static async Task WriteJsonAsync(HttpResponseData response, object? value, CancellationToken cancellationToken)
    {
        response.Headers.Add("Content-Type", "application/json");
        var json = JsonSerializer.Serialize(value);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        await response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        response.Body.Position = 0;
    }
}