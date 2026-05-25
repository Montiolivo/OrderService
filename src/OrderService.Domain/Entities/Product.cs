using OrderService.Domain.Exceptions;

namespace OrderService.Domain.Entities;

/// <summary>
/// Representa um produto com controle de estoque disponível.
/// </summary>
public class Product : Entity
{
    public string Name { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; }
    public int AvailableQuantity { get; private set; }

    // EF Core constructor
    private Product() : base()
    {
        Name = string.Empty;
        Currency = string.Empty;
    }

    public Product(Guid id, string name, decimal unitPrice, string currency, int availableQuantity)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");

        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        if (availableQuantity < 0)
            throw new DomainException("Available quantity cannot be negative.");

        Name = name;
        UnitPrice = unitPrice;
        Currency = currency.ToUpperInvariant();
        AvailableQuantity = availableQuantity;
    }

    /// <summary>
    /// Reserva estoque para um pedido. Lança exceção se estoque insuficiente.
    /// </summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity to reserve must be greater than zero.");

        if (AvailableQuantity < quantity)
            throw new InsufficientStockException(Id, quantity, AvailableQuantity);

        AvailableQuantity -= quantity;
    }

    /// <summary>
    /// Libera estoque previamente reservado (ex: cancelamento de pedido).
    /// </summary>
    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity to release must be greater than zero.");

        AvailableQuantity += quantity;
    }

    public bool HasSufficientStock(int quantity) => AvailableQuantity >= quantity;
}