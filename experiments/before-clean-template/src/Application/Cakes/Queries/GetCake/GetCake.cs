using LayerCake.CleanTemplate.Application.Common.Interfaces;

namespace LayerCake.CleanTemplate.Application.Cakes.Queries.GetCake;

public record GetCakeQuery(Guid Id) : IRequest<CakeDto>;

public class GetCakeQueryHandler : IRequestHandler<GetCakeQuery, CakeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCakeQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CakeDto> Handle(GetCakeQuery request, CancellationToken cancellationToken)
    {
        var cake = await _context.Cakes
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .ProjectTo<CakeDto>(_mapper.ConfigurationProvider)
            .SingleOrDefaultAsync(cancellationToken);

        Guard.Against.NotFound(request.Id, cake);

        return cake;
    }
}
