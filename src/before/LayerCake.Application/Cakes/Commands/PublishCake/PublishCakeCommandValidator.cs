using FluentValidation;

namespace LayerCake.Application.Cakes.Commands.PublishCake;

/// <summary>
/// Runs automatically via the MediatR validation pipeline behavior; the
/// handler never sees an invalid command.
/// </summary>
public sealed class PublishCakeCommandValidator : AbstractValidator<PublishCakeCommand>
{
    public PublishCakeCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();

        RuleFor(c => c.Price)
            .GreaterThan(0);
    }
}
