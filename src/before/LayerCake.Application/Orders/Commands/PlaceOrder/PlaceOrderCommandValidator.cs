using FluentValidation;

namespace LayerCake.Application.Orders.Commands.PlaceOrder;

/// <summary>
/// Guard 1 of the contract's ordered chain: lines must be non-empty and
/// every quantity at least 1. Runs automatically via the MediatR validation
/// pipeline behavior, so it always fires before the handler's 422 guards.
/// </summary>
public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(c => c.Lines)
            .NotEmpty();

        RuleForEach(c => c.Lines)
            .ChildRules(line => line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(1));
    }
}
