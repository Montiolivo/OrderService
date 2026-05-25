using OrderService.Application.DTOs;
using OrderService.Domain.Entities;

namespace OrderService.Application.Common;

public static class OrderMapper
{
    public static OrderDto ToDto(Order order)
    {
        var items = order.Items
            .Select(ToDto)
            .ToList()
            .AsReadOnly();

        return new OrderDto(
            Id: order.Id,
            CustomerId: order.CustomerId,
            Status: order.Status.ToString(),
            Currency: order.Currency,
            Total: order.Total().Amount,
            Items: items,
            CreatedAt: order.CreatedAt,
            ConfirmedAt: order.ConfirmedAt,
            CanceledAt: order.CanceledAt
        );
    }

    public static OrderItemDto ToDto(OrderItem item) =>
        new(
            Id: item.Id,
            ProductId: item.ProductId,
            ProductName: item.ProductName,
            UnitPrice: item.UnitPrice,
            Currency: item.Currency,
            Quantity: item.Quantity,
            Subtotal: item.Subtotal().Amount
        );
}
