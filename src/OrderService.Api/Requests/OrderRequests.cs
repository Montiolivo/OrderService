namespace OrderService.Api.Requests;

public record CreateOrderRequest(
    Guid CustomerId,
    string Currency,
    IReadOnlyList<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    Guid ProductId,
    int Quantity
);
