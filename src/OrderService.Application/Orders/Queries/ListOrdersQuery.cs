using MediatR;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Enums;

namespace OrderService.Application.Orders.Queries.ListOrders;

public record ListOrdersQuery(
    Guid? CustomerId,
    OrderStatus? Status,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize
) : IRequest<PagedResult<OrderDto>>;

public sealed class ListOrdersHandler : IRequestHandler<ListOrdersQuery, PagedResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public ListOrdersHandler(IOrderRepository orderRepository)
        => _orderRepository = orderRepository;

    public async Task<PagedResult<OrderDto>> Handle(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _orderRepository.ListAsync(
            customerId: query.CustomerId,
            status: query.Status,
            from: query.From,
            to: query.To,
            page: query.Page,
            pageSize: query.PageSize,
            cancellationToken: cancellationToken
        );

        var dtos = items.Select(OrderMapper.ToDto).ToList().AsReadOnly();

        return new PagedResult<OrderDto>(
            Items: dtos,
            TotalCount: totalCount,
            Page: query.Page,
            PageSize: query.PageSize
        );
    }
}
