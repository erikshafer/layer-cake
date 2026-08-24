using LayerCake.Slices.Cakes;
using Wolverine.Http;
using Wolverine.Persistence;

namespace LayerCake.Slices.Features;

public static class GetCakeEndpoint
{
    // [Entity] loads the cake from the {id} route value; a miss becomes a
    // ProblemDetails 404 (matching the before twin's error shape) before the
    // method body ever runs.
    [WolverineGet("/cakes/{id}")]
    public static Cake Get([Entity(Required = true, OnMissing = OnMissing.ProblemDetailsWith404)] Cake cake)
        => cake;
}
