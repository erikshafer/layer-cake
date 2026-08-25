using AutoMapper;
using LayerCake.Application.Common.Interfaces;
using MediatR;

namespace LayerCake.Application.Baker.Queries.GetBakerTasks;

public sealed class GetBakerTasksQueryHandler : IRequestHandler<GetBakerTasksQuery, List<BakerTaskDto>>
{
    private readonly IBakerTaskRepository _bakerTaskRepository;
    private readonly IMapper _mapper;

    public GetBakerTasksQueryHandler(IBakerTaskRepository bakerTaskRepository, IMapper mapper)
    {
        _bakerTaskRepository = bakerTaskRepository;
        _mapper = mapper;
    }

    public async Task<List<BakerTaskDto>> Handle(GetBakerTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _bakerTaskRepository.GetAllAsync(request.OrderId, cancellationToken);

        return _mapper.Map<List<BakerTaskDto>>(tasks);
    }
}
