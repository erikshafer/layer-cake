namespace LayerCake.CleanTemplate.Application.Cakes.Commands.PublishCake;

public class PublishCakeCommandValidator : AbstractValidator<PublishCakeCommand>
{
    public PublishCakeCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(v => v.Price)
            .GreaterThan(0);
    }
}
