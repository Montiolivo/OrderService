using MediatR;
using OrderService.Application.DTOs;

namespace OrderService.Application.Orders.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId) : IRequest<OrderDto>;
