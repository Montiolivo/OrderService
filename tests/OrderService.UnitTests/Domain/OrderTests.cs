using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;
using Xunit;

namespace OrderService.UnitTests.Domain.Entities;

public class OrderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Order CreatePlacedOrder()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");
        order.AddItem(Guid.NewGuid(), "Product A", 100m, "BRL", 2);
        return order;
    }

    private static Order CreateConfirmedOrder()
    {
        var order = CreatePlacedOrder();
        order.Confirm();
        return order;
    }

    // ── Order.Create ──────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldCreateOrder_WithPlacedStatus()
    {
        var customerId = Guid.NewGuid();

        var order = Order.Create(customerId, "BRL");

        order.Id.Should().NotBeEmpty();
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Placed);
        order.Currency.Should().Be("BRL");
        order.Items.Should().BeEmpty();
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldThrow_WhenCustomerIdIsEmpty()
    {
        var act = () => Order.Create(Guid.Empty, "BRL");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ShouldNormalizeCurrencyToUpperCase()
    {
        var order = Order.Create(Guid.NewGuid(), "brl");

        order.Currency.Should().Be("BRL");
    }

    // ── AddItem ───────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_ShouldAddItemToOrder()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");
        var productId = Guid.NewGuid();

        order.AddItem(productId, "Notebook", 4999.99m, "BRL", 1);

        order.Items.Should().HaveCount(1);
        order.Items.First().ProductId.Should().Be(productId);
        order.Items.First().Quantity.Should().Be(1);
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenDuplicateProduct()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");
        var productId = Guid.NewGuid();
        order.AddItem(productId, "Product", 100m, "BRL", 1);

        var act = () => order.AddItem(productId, "Product", 100m, "BRL", 1);

        act.Should().Throw<DomainException>().WithMessage("*already in the order*");
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenCurrencyMismatch()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");

        var act = () => order.AddItem(Guid.NewGuid(), "Product", 100m, "USD", 1);

        act.Should().Throw<DomainException>().WithMessage("*currency*");
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenOrderIsConfirmed()
    {
        var order = CreateConfirmedOrder();

        var act = () => order.AddItem(Guid.NewGuid(), "Product", 100m, "BRL", 1);

        act.Should().Throw<DomainException>().WithMessage("*Cannot modify*");
    }

    // ── Total ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Total_ShouldReturnZero_WhenNoItems()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");

        var total = order.Total();

        total.Amount.Should().Be(0m);
    }

    [Fact]
    public void Total_ShouldCalculateCorrectly_WithMultipleItems()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");
        order.AddItem(Guid.NewGuid(), "Product A", 100m, "BRL", 2);
        order.AddItem(Guid.NewGuid(), "Product B", 50m, "BRL", 3);

        var total = order.Total();

        total.Amount.Should().Be(350m);
        total.Currency.Should().Be("BRL");
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_ShouldTransitionToConfirmed_WhenPlaced()
    {
        var order = CreatePlacedOrder();

        var transitioned = order.Confirm();

        transitioned.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Confirm_ShouldBeIdempotent_WhenAlreadyConfirmed()
    {
        var order = CreatePlacedOrder();
        order.Confirm();

        var transitioned = order.Confirm();

        transitioned.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_ShouldThrow_WhenCanceled()
    {
        var order = CreatePlacedOrder();
        order.Cancel();

        var act = () => order.Confirm();

        act.Should().Throw<DomainException>().WithMessage("*cannot be confirmed*");
    }

    [Fact]
    public void Confirm_ShouldThrow_WhenNoItems()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");

        var act = () => order.Confirm();

        act.Should().Throw<DomainException>().WithMessage("*no items*");
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_ShouldTransitionToCanceled_WhenPlaced()
    {
        var order = CreatePlacedOrder();

        var result = order.Cancel();

        result.Should().Be(CancelResult.CanceledFromPlaced);
        order.Status.Should().Be(OrderStatus.Canceled);
        order.CanceledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldTransitionToCanceled_WhenConfirmed()
    {
        var order = CreateConfirmedOrder();

        var result = order.Cancel();

        result.Should().Be(CancelResult.CanceledFromConfirmed);
        order.Status.Should().Be(OrderStatus.Canceled);
    }

    [Fact]
    public void Cancel_ShouldBeIdempotent_WhenAlreadyCanceled()
    {
        var order = CreatePlacedOrder();
        order.Cancel();

        var result = order.Cancel();

        result.Should().Be(CancelResult.AlreadyCanceled);
        order.Status.Should().Be(OrderStatus.Canceled);
    }

    [Fact]
    public void Cancel_ShouldReturnCanceledFromConfirmed_SoHandlerKnowsToReleaseStock()
    {
        var order = CreateConfirmedOrder();

        var result = order.Cancel();

        result.Should().Be(CancelResult.CanceledFromConfirmed);
    }

    // ── Timestamps ────────────────────────────────────────────────────────────

    [Fact]
    public void ConfirmedAt_ShouldBeSet_WhenConfirmed()
    {
        var order = CreatePlacedOrder();

        order.Confirm();

        order.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CanceledAt_ShouldBeSet_WhenCanceled()
    {
        var order = CreatePlacedOrder();

        order.Cancel();

        order.CanceledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
