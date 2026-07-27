using System;
using System.Collections.Generic;
using System.Text;

namespace Contoso.InventoryFunctions.Contracts
{
    public sealed record ReleaseInventoryResponse(
        bool Success,
        string OrderId,
        string ReservationId,
        string? ErrorCode,
        string? Message);
}
