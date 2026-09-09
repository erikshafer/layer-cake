using System.Reflection;
using Marten;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace LayerCake.Slices.Cakes;

public record PublishCake(string? Name, string? Description, decimal Price);

/// <summary>
/// 201 response for a freshly published cake. IHttpAware lets the response
/// type write its own status code and Location header, like Wolverine's
/// CreationResponse but without serializing an extra url field into the body.
/// </summary>
public record PublishedCake(Guid Id, string Name, string Description, decimal Price, DateTimeOffset PublishedAt)
    : IHttpAware
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        => builder.Metadata.Add(new ProducesResponseTypeMetadata(201, typeof(PublishedCake), ["application/json"]));

    void IHttpAware.Apply(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status201Created;
        context.Response.Headers.Location = $"/cakes/{Id}";
    }
}

public static class PublishCakeEndpoint
{
    // Wolverine runs ValidateAsync before Post and short-circuits with the
    // ProblemDetails (as application/problem+json) so the endpoint method
    // below only ever sees the happy path.
    public static async Task<ProblemDetails> ValidateAsync(
        PublishCake command,
        IQuerySession session,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new ProblemDetails { Detail = "Name is required", Status = 400 };
        }

        if (command.Price <= 0)
        {
            return new ProblemDetails { Detail = "Price must be greater than zero", Status = 400 };
        }

        var nameTaken = await session.Query<Cake>().AnyAsync(c => c.Name == command.Name, ct);

        return nameTaken
            ? new ProblemDetails { Detail = $"A cake named \"{command.Name}\" has already been published", Status = 409 }
            : WolverineContinue.NoProblems;
    }

    [WolverinePost("/cakes")]
    public static PublishedCake Post(PublishCake command, IDocumentSession session)
    {
        var cake = new Cake
        {
            Id = Guid.NewGuid(),
            Name = command.Name!,
            Description = command.Description ?? string.Empty,
            Price = command.Price,
            PublishedAt = DateTimeOffset.UtcNow,
        };

        // AutoApplyTransactions commits this; no SaveChangesAsync in handlers.
        session.Store(cake);

        return new PublishedCake(cake.Id, cake.Name, cake.Description, cake.Price, cake.PublishedAt);
    }
}
