using Microsoft.AspNetCore.Http.HttpResults;

namespace LayerCake.CleanTemplate.Web.Endpoints;

public class Ping : IEndpointGroup
{
    public static string? RoutePrefix => "/ping";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetPing);
    }

    [EndpointSummary("Ping")]
    [EndpointDescription("Liveness probe shared with the two LayerCake twins.")]
    public static ContentHttpResult GetPing()
    {
        return TypedResults.Text("pong");
    }
}
