namespace OrderService.Application.DTOs;

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? CanceledAt
);

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal Subtotal
);
