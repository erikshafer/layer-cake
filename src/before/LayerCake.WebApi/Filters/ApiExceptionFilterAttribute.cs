using LayerCake.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LayerCake.WebApi.Filters;

/// <summary>
/// Translates Application-layer exceptions into RFC 7807 problem responses
/// so controllers stay free of error mapping.
/// </summary>
public sealed class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<ApiExceptionFilterAttribute> _logger;

    public ApiExceptionFilterAttribute(ILogger<ApiExceptionFilterAttribute> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ValidationException validationException:
                Handle(context, new ValidationProblemDetails(validationException.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                });
                break;

            case NotFoundException notFoundException:
                Handle(context, new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "The specified resource was not found.",
                    Detail = notFoundException.Message
                });
                break;

            case DuplicateCakeNameException duplicateCakeNameException:
                Handle(context, new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = duplicateCakeNameException.Message
                });
                break;

            case UnknownCakesException unknownCakesException:
                Handle(context, new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "The order references unknown cakes.",
                    Detail = unknownCakesException.Message
                });
                break;

            case InvalidCouponException invalidCouponException:
                Handle(context, new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "The order's coupon is not valid.",
                    Detail = invalidCouponException.Message
                });
                break;

            default:
                // Anything unmapped (a DbUpdateException from a length constraint,
                // a lost unique-index race) still answers as problem+json rather
                // than a bare 500. The exception goes to the log, not the body.
                _logger.LogError(context.Exception, "Unhandled exception for {Path}", context.HttpContext.Request.Path);
                Handle(context, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An error occurred while processing your request."
                });
                break;
        }
    }

    private static void Handle(ExceptionContext context, ProblemDetails problemDetails)
    {
        var result = new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
        result.ContentTypes.Add("application/problem+json");

        context.Result = result;
        context.ExceptionHandled = true;
    }
}
