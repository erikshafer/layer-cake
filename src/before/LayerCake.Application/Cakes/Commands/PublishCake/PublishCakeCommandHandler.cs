using AutoMapper;
using LayerCake.Application.Common.Exceptions;
using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using MediatR;

namespace LayerCake.Application.Cakes.Commands.PublishCake;

public sealed class PublishCakeCommandHandler : IRequestHandler<PublishCakeCommand, CakeDto>
{
    private readonly ICakeRepository _cakeRepository;
    private readonly IMapper _mapper;

    public PublishCakeCommandHandler(ICakeRepository cakeRepository, IMapper mapper)
    {
        _cakeRepository = cakeRepository;
        _mapper = mapper;
    }

    public async Task<CakeDto> Handle(PublishCakeCommand request, CancellationToken cancellationToken)
    {
        if (await _cakeRepository.ExistsWithNameAsync(request.Name!, cancellationToken))
        {
            throw new DuplicateCakeNameException(request.Name!);
        }

        var cake = new Cake
        {
            Id = Guid.NewGuid(),
            Name = request.Name!,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            PublishedAt = DateTimeOffset.UtcNow
        };

        await _cakeRepository.AddAsync(cake, cancellationToken);

        return _mapper.Map<CakeDto>(cake);
    }
}
