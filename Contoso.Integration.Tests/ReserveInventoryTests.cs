using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Contoso.InventoryFunctions;

namespace Contoso.Integration.Tests;

public class ReserveInventoryTests
{
    private static HttpRequest CreateHttpRequest(string json)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        var bytes = Encoding.UTF8.GetBytes(json ?? string.Empty);
        request.Body = new MemoryStream(bytes);
        request.ContentLength = bytes.Length;
        request.ContentType = "application/json";
        return request;
    }

    [Fact]
    public async Task Run_Returns_Ok_For_Valid_Request()
    {
        var payload = "{\"OrderId\":\"ORD-1\",\"ProductId\":\"PROD-1\",\"Quantity\":5}";
        var request = CreateHttpRequest(payload);
        var function = new ReserveInventory(new NullLogger<ReserveInventory>());

        var result = await function.Run(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ReserveInventoryResponse>(ok.Value!);
        Assert.True(response.Success);
        Assert.Equal("ORD-1", response.OrderId);
        Assert.Equal("PROD-1", response.ProductId);
        Assert.Equal(5, response.QuantityReserved);
        Assert.Equal(15, response.RemainingInventory);
    }

    [Fact]
    public async Task Run_Returns_BadRequest_For_Invalid_Json()
    {
        var payload = "{ bad json }";
        var request = CreateHttpRequest(payload);
        var function = new ReserveInventory(new NullLogger<ReserveInventory>());

        var result = await function.Run(request, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value!);
        Assert.Equal("INVALID_REQUEST", error.ErrorCode);
    }

    [Fact]
    public async Task Run_Returns_BadRequest_For_Missing_Fields()
    {
        var payload = "{\"OrderId\":\"\",\"ProductId\":\"\",\"Quantity\":0}";
        var request = CreateHttpRequest(payload);
        var function = new ReserveInventory(new NullLogger<ReserveInventory>());

        var result = await function.Run(request, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(bad.Value!);
        Assert.Equal("INVALID_REQUEST", error.ErrorCode);
    }

    [Fact]
    public async Task Run_Returns_Conflict_When_Not_Enough_Inventory()
    {
        var payload = "{\"OrderId\":\"ORD-2\",\"ProductId\":\"PROD-2\",\"Quantity\":999}";
        var request = CreateHttpRequest(payload);
        var function = new ReserveInventory(new NullLogger<ReserveInventory>());

        var result = await function.Run(request, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(conflict.Value!);
        Assert.Equal("OUT_OF_STOCK", error.ErrorCode);
    }
}
