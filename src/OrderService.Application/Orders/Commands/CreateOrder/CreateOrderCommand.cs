using MediatR;
using OrderService.Application.DTOs;

namespace OrderService.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid CustomerId,
    string Currency,
    IReadOnlyList<CreateOrderItemCommand> Items
) : IRequest<OrderDto>;

public record CreateOrderItemCommand(
    Guid ProductId,
    int Quantity
);
