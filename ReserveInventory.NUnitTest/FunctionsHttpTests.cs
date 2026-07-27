using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Contoso.InventoryFunctions.Functions;
using Contoso.InventoryFunctions.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ReserveInventory.NUnitTest;

[TestFixture]
public class FunctionsHttpTests
{
    private FunctionContext _ctx = null!;

    [SetUp]
    public void Setup()
    {
        var ctxMock = new Mock<FunctionContext>();
        // Ensure InstanceServices is non-null so HttpResponseDataExtensions can resolve or fallback to default serializer
        // Provide a minimal IServiceProvider so HttpResponseDataExtensions.InstanceServices is not null
        var spMock = new Mock<System.IServiceProvider>();
        // If the worker asks for any serializer-like interface, or for IOptions<WorkerOptions>, return appropriate objects.
        spMock.Setup(s => s.GetService(It.IsAny<Type>())).Returns((Type t) =>
        {
            try
            {
                // If IOptions<WorkerOptions> requested, create OptionsWrapper<WorkerOptions> with Serializer set
                if (t != null && t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Options.IOptions<>))
                {
                    var genericArg = t.GetGenericArguments()[0];
                    if (genericArg != null && genericArg.Name == "WorkerOptions")
                    {
                        var woInstance = Activator.CreateInstance(genericArg);

                        // Find a serializer interface (prefer exact name 'ObjectSerializer')
                        var objSerializerInterface = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a =>
                            {
                                try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                            })
                            .FirstOrDefault(ty => ty.IsInterface && ty.Name == "ObjectSerializer")
                            ?? AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a =>
                                {
                                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                                })
                                .FirstOrDefault(ty => ty.IsInterface && ty.Name.IndexOf("Serializer", StringComparison.OrdinalIgnoreCase) >= 0);

                        if (objSerializerInterface != null)
                        {
                            var serProxy = typeof(DispatchProxy)
                                .GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!
                                .MakeGenericMethod(objSerializerInterface, typeof(SerializerProxy))
                                .Invoke(null, null);

                            var serializerProp = genericArg.GetProperty("Serializer");
                            if (serializerProp != null)
                            {
                                serializerProp.SetValue(woInstance, serProxy);
                            }
                        }

                        var optionsWrapperType = typeof(Microsoft.Extensions.Options.OptionsWrapper<>).MakeGenericType(genericArg);
                        var optionsWrapper = Activator.CreateInstance(optionsWrapperType, woInstance);
                        return optionsWrapper;
                    }
                }

                // Fallback: if a serializer-like interface itself is requested, return a DispatchProxy implementation.
                if (t != null && t.IsInterface && t.Name.IndexOf("Serializer", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var proxy = typeof(DispatchProxy)
                        .GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!
                        .MakeGenericMethod(t, typeof(SerializerProxy))
                        .Invoke(null, null);
                    return proxy;
                }
            }
            catch
            {
                // ignore and fall through
            }

            return null;
        });
        ctxMock.SetupGet(c => c.InstanceServices).Returns(spMock.Object);
        _ctx = ctxMock.Object;
    }

    [Test]
    public async Task ReleaseInventory_Success_ReturnsOk()
    {
        var svcMock = new Mock<Contoso.InventoryFunctions.Services.IInventoryService>();
        svcMock.Setup(s => s.ReleaseAsync(It.IsAny<ReleaseInventoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReleaseInventoryResponse(true, "ORD-1", "RES-1", null, null));

        var func = new ReleaseInventory(svcMock.Object, new NullLogger<ReleaseInventory>());

        var requestObj = new ReleaseInventoryRequest("ORD-1", "RES-1");
        var json = JsonSerializer.Serialize(requestObj);

        var req = new ReserveInventory.NUnitTest.TestHelpers.TestHttpRequestData(_ctx, json);
        var resp = await func.RunAsync(req, CancellationToken.None);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = string.Empty;
        using (var reader = new StreamReader(resp.Body, Encoding.UTF8, leaveOpen: true))
        {
            resp.Body.Position = 0;
            body = reader.ReadToEnd();
        }
        var result = JsonSerializer.Deserialize<ReleaseInventoryResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Success, Is.True);
    }

    [Test]
    public async Task ReleaseInventory_ReservationNotFound_ReturnsNotFound()
    {
        var svcMock = new Mock<Contoso.InventoryFunctions.Services.IInventoryService>();
        svcMock.Setup(s => s.ReleaseAsync(It.IsAny<ReleaseInventoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReleaseInventoryResponse(false, "ORD-1", "RES-UNKNOWN", "RESERVATION_NOT_FOUND", "The inventory reservation was not found."));

        var func = new ReleaseInventory(svcMock.Object, new NullLogger<ReleaseInventory>());

        var requestObj = new ReleaseInventoryRequest("ORD-1", "RES-UNKNOWN");
        var json = JsonSerializer.Serialize(requestObj);

        var req = new ReserveInventory.NUnitTest.TestHelpers.TestHttpRequestData(_ctx, json);
        var resp = await func.RunAsync(req, CancellationToken.None);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        string body;
        using (var reader = new StreamReader(resp.Body, Encoding.UTF8, leaveOpen: true))
        {
            resp.Body.Position = 0;
            body = reader.ReadToEnd();
        }
        Assert.That(body, Does.Contain("RESERVATION_NOT_FOUND").Or.Contain("reservation"));
    }

    [Test]
    public async Task ReleaseInventory_InvalidJson_ReturnsBadRequest()
    {
        var svcMock = new Mock<Contoso.InventoryFunctions.Services.IInventoryService>();
        var func = new ReleaseInventory(svcMock.Object, new NullLogger<ReleaseInventory>());

        var reqMock = new Mock<HttpRequestData>(MockBehavior.Strict, _ctx);
        var reqBody = new MemoryStream(Encoding.UTF8.GetBytes("{ bad json }"));
        reqMock.SetupGet(r => r.Body).Returns(reqBody);
        reqMock.Setup(r => r.CreateResponse())
            .Returns(() =>
            {
                var respMock = new Mock<HttpResponseData>(_ctx);
                var outStream = new MemoryStream();
                respMock.SetupGet(x => x.Body).Returns(outStream);
                respMock.SetupProperty(x => x.StatusCode);
                respMock.Setup(r => r.WriteAsJsonAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Returns<object, CancellationToken>((obj, ct) =>
                    {
                        var jsonOut = JsonSerializer.Serialize(obj);
                        var bytes = Encoding.UTF8.GetBytes(jsonOut);
                        outStream.Write(bytes, 0, bytes.Length);
                        outStream.Position = 0;
                        return new ValueTask(Task.CompletedTask);
                    });

                return respMock.Object;
            });

        var resp = await func.RunAsync(reqMock.Object, CancellationToken.None);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        string body;
        using (var reader = new StreamReader(resp.Body, Encoding.UTF8, leaveOpen: true))
        {
            resp.Body.Position = 0;
            body = reader.ReadToEnd();
        }
        Assert.That(body, Does.Contain("INVALID_JSON"));
    }

    [Test]
    public async Task AuthorizePayment_Success_ReturnsOk()
    {
        var svcMock = new Mock<Contoso.InventoryFunctions.Services.IPaymentService>();
        svcMock.Setup(s => s.AuthorizeAsync(It.IsAny<AuthorizePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizePaymentResponse(true, "AUTH-1", "ORD-10", 10m, null, null));

        var func = new AuthorizePayment(svcMock.Object, new NullLogger<AuthorizePayment>());

        var requestObj = new AuthorizePaymentRequest("ORD-10", "PAY-APPROVED", 10m);
        var json = JsonSerializer.Serialize(requestObj);

        var req = new ReserveInventory.NUnitTest.TestHelpers.TestHttpRequestData(_ctx, json);
        var resp = await func.RunAsync(req, CancellationToken.None);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        string body;
        using (var reader = new StreamReader(resp.Body, Encoding.UTF8, leaveOpen: true))
        {
            resp.Body.Position = 0;
            body = reader.ReadToEnd();
        }
        var result = JsonSerializer.Deserialize<AuthorizePaymentResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Success, Is.True);
    }

    [Test]
    public async Task AuthorizePayment_Declined_ReturnsConflict()
    {
        var svcMock = new Mock<Contoso.InventoryFunctions.Services.IPaymentService>();
        svcMock.Setup(s => s.AuthorizeAsync(It.IsAny<AuthorizePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizePaymentResponse(false, null, "ORD-11", 5m, "PAYMENT_DECLINED", "The payment provider declined the transaction."));

        var func = new AuthorizePayment(svcMock.Object, new NullLogger<AuthorizePayment>());

        var requestObj = new AuthorizePaymentRequest("ORD-11", "PAY-DECLINED", 5m);
        var json = JsonSerializer.Serialize(requestObj);

        var req = new ReserveInventory.NUnitTest.TestHelpers.TestHttpRequestData(_ctx, json);
        var resp = await func.RunAsync(req, CancellationToken.None);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        string body;
        using (var reader = new StreamReader(resp.Body, Encoding.UTF8, leaveOpen: true))
        {
            resp.Body.Position = 0;
            body = reader.ReadToEnd();
        }
        Assert.That(body, Does.Contain("PAYMENT_DECLINED").Or.Contain("declined"));
    }

    [Test]
    public async Task AuthorizePayment_InvalidAmount_ReturnsBadRequest()
    {
        var svcMock = new Mock<Contoso.InventoryFunctions.Services.IPaymentService>();
        var func = new AuthorizePayment(svcMock.Object, new NullLogger<AuthorizePayment>());

        var requestObj = new AuthorizePaymentRequest("ORD-12", "PAY-APPROVED", 0m);
        var json = JsonSerializer.Serialize(requestObj);

        var reqMock = new Mock<HttpRequestData>(MockBehavior.Strict, _ctx);
        var reqBody = new MemoryStream(Encoding.UTF8.GetBytes(json));
        reqMock.SetupGet(r => r.Body).Returns(reqBody);
        reqMock.Setup(r => r.CreateResponse())
            .Returns(() =>
            {
                var respMock = new Mock<HttpResponseData>(_ctx);
                var outStream = new MemoryStream();
                respMock.SetupGet(x => x.Body).Returns(outStream);
                respMock.SetupProperty(x => x.StatusCode);
                return respMock.Object;
            });

        var resp = await func.RunAsync(reqMock.Object, CancellationToken.None);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        string body;
        using (var reader = new StreamReader(resp.Body, Encoding.UTF8, leaveOpen: true))
        {
            resp.Body.Position = 0;
            body = reader.ReadToEnd();
        }
        Assert.That(body, Does.Contain("INVALID_AMOUNT"));
    }
}

// OptionsProxy moved to top-level to avoid nested type in Setup
public class OptionsProxy : DispatchProxy
{
    public object? ValueInstance { get; set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null) return null;
        if (targetMethod.Name == "get_Value")
        {
            return ValueInstance;
        }
        return null;
    }
}

// Serializer proxy used to satisfy worker's serializer resolution for tests.
public class SerializerProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo targetMethod, object[] args)
    {
        Stream? stream = null;
        object? value = null;
        if (args != null)
        {
            foreach (var a in args)
            {
                if (a is Stream s) { stream = s; break; }
            }
            foreach (var a in args)
            {
                if (a is Stream || a is CancellationToken || a is Type) continue;
                value = a;
                break;
            }
        }

        if (stream != null)
        {
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var bytes = Encoding.UTF8.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
            stream.Position = 0;
        }

        var rt = targetMethod?.ReturnType;
        if (rt == null || rt == typeof(void)) return null;
        if (rt == typeof(Task)) return Task.CompletedTask;
        if (rt == typeof(ValueTask)) return new ValueTask(Task.CompletedTask);
        if (rt.IsGenericType)
        {
            var genDef = rt.GetGenericTypeDefinition();
            if (genDef == typeof(Task<>))
            {
                var resultType = rt.GetGenericArguments()[0];
                var defaultValue = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                var fromResult = typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType);
                return fromResult.Invoke(null, new[] { defaultValue });
            }
            if (genDef == typeof(ValueTask<>))
            {
                var resultType = rt.GetGenericArguments()[0];
                var defaultValue = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                return Activator.CreateInstance(rt, new[] { defaultValue });
            }
        }

        return null;
    }
}
