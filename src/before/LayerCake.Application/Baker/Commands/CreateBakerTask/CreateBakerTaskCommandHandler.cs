using LayerCake.Application.Common.Interfaces;
using LayerCake.Domain.Entities;
using MediatR;

namespace LayerCake.Application.Baker.Commands.CreateBakerTask;

public sealed class CreateBakerTaskCommandHandler : IRequestHandler<CreateBakerTaskCommand>
{
    private readonly IBakerTaskRepository _bakerTaskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBakerTaskCommandHandler(IBakerTaskRepository bakerTaskRepository, IUnitOfWork unitOfWork)
    {
        _bakerTaskRepository = bakerTaskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreateBakerTaskCommand request, CancellationToken cancellationToken)
    {
        // The broker delivers at least once; the contract promises exactly one
        // task per order. A redelivered message must find its work already done.
        if (await _bakerTaskRepository.ExistsForOrderAsync(request.OrderId, cancellationToken))
        {
            return;
        }

        _bakerTaskRepository.Add(new BakerTask
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            Summary = request.Summary
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
