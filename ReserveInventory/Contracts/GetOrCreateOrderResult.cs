namespace Contoso.InventoryFunctions.Contracts;
public sealed record GetOrCreateOrderResult(
    OrderProcessingEntity Entity,
    bool Created);
