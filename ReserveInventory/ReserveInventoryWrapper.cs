using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Contoso.InventoryFunctions;

// Compatibility wrapper so tests that reference Contoso.InventoryFunctions.ReserveInventory
// (without the .Functions namespace) continue to work.
public sealed class ReserveInventory
{
    private readonly Contoso.InventoryFunctions.Functions.ReserveInventory _impl;

    public ReserveInventory(Microsoft.Extensions.Logging.ILogger<Contoso.InventoryFunctions.ReserveInventory> logger)
    {
        // The inner implementation has a back-compat ctor that accepts a logger.
        _impl = new Contoso.InventoryFunctions.Functions.ReserveInventory(new NullLogger<Contoso.InventoryFunctions.Functions.ReserveInventory>());
    }

    public Task<IActionResult> Run(HttpRequest request, CancellationToken cancellationToken)
        => _impl.Run(request, cancellationToken);
}
