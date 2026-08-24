using AutoMapper;
using LayerCake.Application.Common.Interfaces;
using MediatR;

namespace LayerCake.Application.Cakes.Queries.BrowseCakes;

public sealed class BrowseCakesQueryHandler : IRequestHandler<BrowseCakesQuery, List<CakeDto>>
{
    private readonly ICakeRepository _cakeRepository;
    private readonly IMapper _mapper;

    public BrowseCakesQueryHandler(ICakeRepository cakeRepository, IMapper mapper)
    {
        _cakeRepository = cakeRepository;
        _mapper = mapper;
    }

    public async Task<List<CakeDto>> Handle(BrowseCakesQuery request, CancellationToken cancellationToken)
    {
        var cakes = await _cakeRepository.GetAllAsync(cancellationToken);

        return _mapper.Map<List<CakeDto>>(cakes);
    }
}
