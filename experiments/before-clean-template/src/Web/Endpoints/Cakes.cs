using LayerCake.CleanTemplate.Application.Cakes;
using LayerCake.CleanTemplate.Application.Cakes.Commands.PublishCake;
using LayerCake.CleanTemplate.Application.Cakes.Queries.BrowseCakes;
using LayerCake.CleanTemplate.Application.Cakes.Queries.GetCake;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LayerCake.CleanTemplate.Web.Endpoints;

public class Cakes : IEndpointGroup
{
    public static string? RoutePrefix => "/cakes";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(BrowseCakes);
        groupBuilder.MapGet(GetCake, "{id}");
        groupBuilder.MapPost(PublishCake);
    }

    [EndpointSummary("Browse cakes")]
    [EndpointDescription("Retrieves every published cake, ordered by name.")]
    public static async Task<Ok<List<CakeDto>>> BrowseCakes(ISender sender)
    {
        var cakes = await sender.Send(new BrowseCakesQuery());

        return TypedResults.Ok(cakes);
    }

    [EndpointSummary("Get a cake")]
    [EndpointDescription("Retrieves a single published cake by id.")]
    public static async Task<Ok<CakeDto>> GetCake(ISender sender, Guid id)
    {
        var cake = await sender.Send(new GetCakeQuery(id));

        return TypedResults.Ok(cake);
    }

    [EndpointSummary("Publish a cake")]
    [EndpointDescription("Publishes a new cake and returns it. Cake names are unique.")]
    public static async Task<Created<CakeDto>> PublishCake(ISender sender, PublishCakeCommand command)
    {
        var cake = await sender.Send(command);

        return TypedResults.Created($"/cakes/{cake.Id}", cake);
    }
}
