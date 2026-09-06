using LayerCake.CleanTemplate.Application.Common.Interfaces;

namespace LayerCake.CleanTemplate.Application.Cakes.Queries.BrowseCakes;

public record BrowseCakesQuery : IRequest<List<CakeDto>>;

public class BrowseCakesQueryHandler : IRequestHandler<BrowseCakesQuery, List<CakeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public BrowseCakesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<CakeDto>> Handle(BrowseCakesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Cakes
            .AsNoTracking()
            .ProjectTo<CakeDto>(_mapper.ConfigurationProvider)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
