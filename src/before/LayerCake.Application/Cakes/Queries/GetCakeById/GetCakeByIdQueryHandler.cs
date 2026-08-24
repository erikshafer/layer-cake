using AutoMapper;
using LayerCake.Application.Common.Exceptions;
using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using MediatR;

namespace LayerCake.Application.Cakes.Queries.GetCakeById;

public sealed class GetCakeByIdQueryHandler : IRequestHandler<GetCakeByIdQuery, CakeDto>
{
    private readonly ICakeRepository _cakeRepository;
    private readonly IMapper _mapper;

    public GetCakeByIdQueryHandler(ICakeRepository cakeRepository, IMapper mapper)
    {
        _cakeRepository = cakeRepository;
        _mapper = mapper;
    }

    public async Task<CakeDto> Handle(GetCakeByIdQuery request, CancellationToken cancellationToken)
    {
        var cake = await _cakeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Cake), request.Id);

        return _mapper.Map<CakeDto>(cake);
    }
}
