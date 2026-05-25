using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

/// <summary>
/// Aggregate Root do domínio.
/// Toda operação no pedido passa por aqui — nenhuma regra de negócio
/// vive fora desta classe.
/// </summary>
public class Order : Entity
{
    private readonly List<OrderItem> _items = [];

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Currency { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // EF Core constructor
    private Order() : base()
    {
        Currency = string.Empty;
    }

    private Order(Guid id, Guid customerId, string currency) : base(id)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("CustomerId is required.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        CustomerId = customerId;
        Currency = currency.ToUpperInvariant();
        Status = OrderStatus.Placed;
        CreatedAt = DateTime.UtcNow;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria um novo pedido. Pedidos nascem no status Placed.
    /// </summary>
    public static Order Create(Guid customerId, string currency)
        => new(Guid.NewGuid(), customerId, currency);

    // ── Items ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adiciona um item ao pedido. Só permitido em status Draft ou Placed.
    /// </summary>
    public void AddItem(Guid productId, string productName, decimal unitPrice, string currency, int quantity)
    {
        EnsureEditable();

        if (currency.ToUpperInvariant() != Currency)
            throw new DomainException(
                $"Item currency '{currency}' does not match order currency '{Currency}'.");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem is not null)
            throw new DomainException(
                $"Product '{productId}' is already in the order. Remove it first or adjust quantity.");

        var item = new OrderItem(
            id: Guid.NewGuid(),
            orderId: Id,
            productId: productId,
            productName: productName,
            unitPrice: unitPrice,
            currency: currency,
            quantity: quantity
        );

        _items.Add(item);
    }

    // ── Status transitions ────────────────────────────────────────────────────

    /// <summary>
    /// Confirma o pedido. Idempotente: confirmar duas vezes não tem efeito.
    /// Retorna true se houve transição (para controle de baixa de estoque).
    /// </summary>
    public bool Confirm()
    {
        // Idempotência: já confirmado → ignora silenciosamente
        if (Status == OrderStatus.Confirmed)
            return false;

        if (Status != OrderStatus.Placed)
            throw new DomainException(
                $"Order cannot be confirmed from status '{Status}'. Only 'Placed' orders can be confirmed.");

        if (_items.Count == 0)
            throw new DomainException("Cannot confirm an order with no items.");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;

        return true; // houve transição → handler deve baixar estoque
    }

    /// <summary>
    /// Cancela o pedido. Idempotente: cancelar duas vezes não tem efeito.
    /// Retorna true se houve transição de Confirmed → Canceled (para liberar estoque).
    /// </summary>
    public CancelResult Cancel()
    {
        // Idempotência: já cancelado → ignora silenciosamente
        if (Status == OrderStatus.Canceled)
            return CancelResult.AlreadyCanceled;

        if (Status == OrderStatus.Confirmed)
        {
            Status = OrderStatus.Canceled;
            CanceledAt = DateTime.UtcNow;
            return CancelResult.CanceledFromConfirmed; // deve liberar estoque
        }

        if (Status == OrderStatus.Placed)
        {
            Status = OrderStatus.Canceled;
            CanceledAt = DateTime.UtcNow;
            return CancelResult.CanceledFromPlaced; // estoque não foi baixado
        }

        throw new DomainException(
            $"Order cannot be canceled from status '{Status}'.");
    }

    // ── Calculations ──────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula o total do pedido somando os subtotais de todos os itens.
    /// </summary>
    public Money Total()
    {
        if (_items.Count == 0)
            return Money.Zero(Currency);

        return _items
            .Select(i => i.Subtotal())
            .Aggregate((acc, next) => acc.Add(next));
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private void EnsureEditable()
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Placed))
            throw new DomainException(
                $"Cannot modify order in status '{Status}'.");
    }
}

/// <summary>
/// Resultado do cancelamento — informa ao handler se precisa liberar estoque.
/// </summary>
public enum CancelResult
{
    AlreadyCanceled,
    CanceledFromPlaced,
    CanceledFromConfirmed
}
