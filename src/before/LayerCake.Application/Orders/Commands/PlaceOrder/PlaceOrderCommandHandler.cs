using AutoMapper;
using LayerCake.Application.Common.Exceptions;
using LayerCake.Application.Common.Interfaces;
using LayerCake.Application.Coupons;
using LayerCake.Domain.Entities;
using LayerCake.Domain.Enums;
using MediatR;

namespace LayerCake.Application.Orders.Commands.PlaceOrder;

public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    private readonly ICakeRepository _cakeRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ICouponValidationService _couponValidationService;
    private readonly IOrderRepository _orderRepository;
    private readonly IBakerTaskRepository _bakerTaskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PlaceOrderCommandHandler(
        ICakeRepository cakeRepository,
        ICouponRepository couponRepository,
        ICouponValidationService couponValidationService,
        IOrderRepository orderRepository,
        IBakerTaskRepository bakerTaskRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _cakeRepository = cakeRepository;
        _couponRepository = couponRepository;
        _couponValidationService = couponValidationService;
        _orderRepository = orderRepository;
        _bakerTaskRepository = bakerTaskRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // Guard 1 (lines non-empty, quantities >= 1) already ran in the
        // validation pipeline behavior; the guards here continue the
        // contract's order: cakes exist, then coupon valid.
        var lines = request.Lines!;

        // Guard 2: every referenced cake must exist (422 listing offenders).
        var cakeIds = lines.Select(l => l.CakeId).Distinct().ToList();
        var cakes = await _cakeRepository.GetByIdsAsync(cakeIds, cancellationToken);

        var missingIds = cakeIds.Except(cakes.Select(c => c.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new UnknownCakesException(missingIds);
        }

        // Guard 3: a present coupon must evaluate to valid via the shared
        // slice 002 service (422 carrying the failing status). Checkout is
        // authoritative; a bad coupon fails the order, never silently drops.
        Coupon? coupon = null;
        string? canonicalCode = null;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            canonicalCode = request.CouponCode.ToUpperInvariant();
            coupon = await _couponRepository.GetByCodeAsync(canonicalCode, cancellationToken);

            var status = _couponValidationService.Validate(coupon);
            if (status != CouponStatus.Valid)
            {
                throw new InvalidCouponException(canonicalCode, status.ToWireString());
            }
        }

        // Decide: snapshot prices and names from the cakes at placement time.
        var cakesById = cakes.ToDictionary(c => c.Id);
        var orderLines = lines
            .Select(l =>
            {
                var cake = cakesById[l.CakeId];
                return new OrderLine
                {
                    CakeId = cake.Id,
                    Name = cake.Name,
                    UnitPrice = cake.Price,
                    Quantity = l.Quantity,
                    LineTotal = cake.Price * l.Quantity
                };
            })
            .ToList();

        var subtotal = orderLines.Sum(l => l.LineTotal);
        var discount = coupon is null
            ? 0m
            : Math.Round(subtotal * coupon.PercentOff / 100m, 2, MidpointRounding.ToEven);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Lines = orderLines,
            Subtotal = subtotal,
            Discount = discount,
            Total = subtotal - discount,
            CouponCode = canonicalCode,
            PlacedAt = DateTimeOffset.UtcNow
        };

        _orderRepository.Add(order);

        // Notify the baker inline, mid-transaction. Imagine this line is SendGrid.
        _bakerTaskRepository.Add(new BakerTask
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Summary = string.Join(", ", orderLines.Select(l => $"{l.Quantity}x {l.Name}"))
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrderDto>(order);
    }
}
