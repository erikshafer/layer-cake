using LayerCake.CleanTemplate.Application.Common.Exceptions;
using LayerCake.CleanTemplate.Application.Common.Interfaces;
using LayerCake.CleanTemplate.Domain.Entities;

namespace LayerCake.CleanTemplate.Application.Cakes.Commands.PublishCake;

public record PublishCakeCommand : IRequest<CakeDto>
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public decimal Price { get; init; }
}

public class PublishCakeCommandHandler : IRequestHandler<PublishCakeCommand, CakeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly TimeProvider _timeProvider;

    public PublishCakeCommandHandler(IApplicationDbContext context, IMapper mapper, TimeProvider timeProvider)
    {
        _context = context;
        _mapper = mapper;
        _timeProvider = timeProvider;
    }

    public async Task<CakeDto> Handle(PublishCakeCommand request, CancellationToken cancellationToken)
    {
        if (await _context.Cakes.AnyAsync(c => c.Name == request.Name, cancellationToken))
        {
            throw new DuplicateCakeNameException(request.Name!);
        }

        var entity = new Cake
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            PublishedAt = _timeProvider.GetUtcNow()
        };

        _context.Cakes.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CakeDto>(entity);
    }
}
