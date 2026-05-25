using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

/// <summary>
/// Item de um pedido. Pertence exclusivamente ao Aggregate Order.
/// Não deve ser manipulado diretamente — apenas através de Order.
/// </summary>
public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; }
    public int Quantity { get; private set; }

    // EF Core constructor
    private OrderItem() : base()
    {
        ProductName = string.Empty;
        Currency = string.Empty;
    }

    internal OrderItem(Guid id, Guid orderId, Guid productId, string productName,
        decimal unitPrice, string currency, int quantity)
        : base(id)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("OrderItem must belong to an order.");

        if (productId == Guid.Empty)
            throw new DomainException("ProductId is required.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Currency = currency.ToUpperInvariant();
        Quantity = quantity;
    }

    /// <summary>
    /// Calcula o subtotal deste item como Money.
    /// </summary>
    public Money Subtotal() => new Money(UnitPrice, Currency).Multiply(Quantity);
}