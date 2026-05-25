using MediatR;
using OrderService.Application.DTOs;

namespace OrderService.Application.Orders.Commands.ConfirmOrder;

public record ConfirmOrderCommand(Guid OrderId) : IRequest<OrderDto>;
