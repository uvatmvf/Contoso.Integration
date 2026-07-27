using System;
using System.Collections.Generic;
using System.Text;

namespace Contoso.InventoryFunctions.Contracts
{
    public sealed record AuthorizePaymentResponse(
        bool Success,
        string? AuthorizationId,
        string OrderId,
        decimal Amount,
        string? ErrorCode,
        string? Message);
}
