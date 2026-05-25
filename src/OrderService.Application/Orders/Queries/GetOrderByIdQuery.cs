using MediatR;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Exceptions;

namespace OrderService.Application.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<OrderDto> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(query.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", query.OrderId);

        return OrderMapper.ToDto(order);
    }
}
