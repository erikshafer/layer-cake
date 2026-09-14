using Wolverine.Http;
using Wolverine.Persistence;

namespace Tendr.Authorizations;

public static class GetAuthorizationEndpoint
{
    // [Entity] loads the Authorization snapshot by the {id} route value; a
    // miss is a ProblemDetails 404 before the method body runs.
    [WolverineGet("/v1/authorizations/{id}")]
    public static CardAuthorization Get(
        [Entity(Required = true, OnMissing = OnMissing.ProblemDetailsWith404)] Authorization authorization)
        => CardAuthorization.From(authorization);
}
