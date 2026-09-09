using Wolverine.Http;
using Wolverine.Persistence;

namespace LayerCake.Slices.Orders;

public static class GetOrderEndpoint
{
    // [Entity] loads the order from the {id} route value; a miss becomes a
    // ProblemDetails 404 (matching the before twin's error shape) before the
    // method body ever runs. The stored document IS the response: the
    // placement snapshot, never recomputed.
    [WolverineGet("/orders/{id}")]
    public static Order Get([Entity(Required = true, OnMissing = OnMissing.ProblemDetailsWith404)] Order order)
        => order;
}
