using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using Xunit;

namespace OrderService.UnitTests.Domain.Entities;

public class ProductTests
{
    private static Product CreateProduct(int stock = 10) =>
        new(Guid.NewGuid(), "Test Product", 100m, "BRL", stock);

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldCreateProduct_WhenValidArguments()
    {
        var id = Guid.NewGuid();
        var product = new Product(id, "Notebook", 4999.99m, "BRL", 50);

        product.Id.Should().Be(id);
        product.Name.Should().Be("Notebook");
        product.UnitPrice.Should().Be(4999.99m);
        product.Currency.Should().Be("BRL");
        product.AvailableQuantity.Should().Be(50);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsEmpty(string name)
    {
        var act = () => new Product(Guid.NewGuid(), name, 100m, "BRL", 10);

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUnitPriceIsZero()
    {
        var act = () => new Product(Guid.NewGuid(), "Product", 0m, "BRL", 10);

        act.Should().Throw<DomainException>().WithMessage("*price*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAvailableQuantityIsNegative()
    {
        var act = () => new Product(Guid.NewGuid(), "Product", 100m, "BRL", -1);

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    // ── ReserveStock ──────────────────────────────────────────────────────────

    [Fact]
    public void ReserveStock_ShouldDecreaseAvailableQuantity()
    {
        var product = CreateProduct(stock: 10);

        product.ReserveStock(3);

        product.AvailableQuantity.Should().Be(7);
    }

    [Fact]
    public void ReserveStock_ShouldAllowReservingEntireStock()
    {
        var product = CreateProduct(stock: 5);

        product.ReserveStock(5);

        product.AvailableQuantity.Should().Be(0);
    }

    [Fact]
    public void ReserveStock_ShouldThrow_WhenInsufficientStock()
    {
        var product = CreateProduct(stock: 3);

        var act = () => product.ReserveStock(5);

        act.Should().Throw<InsufficientStockException>()
            .WithMessage($"*{product.Id}*");
    }

    [Fact]
    public void ReserveStock_ShouldThrow_WhenQuantityIsZero()
    {
        var product = CreateProduct(stock: 10);

        var act = () => product.ReserveStock(0);

        act.Should().Throw<DomainException>().WithMessage("*greater than zero*");
    }

    // ── ReleaseStock ──────────────────────────────────────────────────────────

    [Fact]
    public void ReleaseStock_ShouldIncreaseAvailableQuantity()
    {
        var product = CreateProduct(stock: 5);
        product.ReserveStock(3);

        product.ReleaseStock(3);

        product.AvailableQuantity.Should().Be(5);
    }

    [Fact]
    public void ReleaseStock_ShouldThrow_WhenQuantityIsZero()
    {
        var product = CreateProduct(stock: 5);

        var act = () => product.ReleaseStock(0);

        act.Should().Throw<DomainException>().WithMessage("*greater than zero*");
    }

    // ── HasSufficientStock ────────────────────────────────────────────────────

    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(10, 5, true)]
    [InlineData(10, 11, false)]
    [InlineData(0, 1, false)]
    public void HasSufficientStock_ShouldReturnCorrectResult(
        int available, int requested, bool expected)
    {
        var product = CreateProduct(stock: available);

        product.HasSufficientStock(requested).Should().Be(expected);
    }
}
