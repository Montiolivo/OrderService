using FluentAssertions;
using OrderService.Domain.ValueObjects;
using Xunit;

namespace OrderService.UnitTests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldCreateMoney_WhenValidArguments()
    {
        var money = new Money(100.50m, "BRL");

        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Constructor_ShouldNormalizeCurrencyToUpperCase()
    {
        var money = new Money(10m, "brl");

        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Constructor_ShouldRoundAmountToTwoDecimalPlaces()
    {
        var money = new Money(10.999m, "BRL");

        money.Amount.Should().Be(11.00m);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAmountIsNegative()
    {
        var act = () => new Money(-1m, "BRL");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrencyIsEmpty()
    {
        var act = () => new Money(10m, "");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Currency*");
    }

    [Fact]
    public void Add_ShouldReturnSum_WhenSameCurrency()
    {
        var a = new Money(100m, "BRL");
        var b = new Money(50m, "BRL");

        var result = a.Add(b);

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Add_ShouldThrow_WhenDifferentCurrencies()
    {
        var brl = new Money(100m, "BRL");
        var usd = new Money(50m, "USD");

        var act = () => brl.Add(usd);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*currencies*");
    }

    [Fact]
    public void Multiply_ShouldReturnCorrectValue()
    {
        var money = new Money(99.90m, "BRL");

        var result = money.Multiply(3);

        result.Amount.Should().Be(299.70m);
        result.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Equality_ShouldBeTrue_WhenSameAmountAndCurrency()
    {
        var a = new Money(100m, "BRL");
        var b = new Money(100m, "BRL");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldBeFalse_WhenDifferentAmount()
    {
        var a = new Money(100m, "BRL");
        var b = new Money(200m, "BRL");

        (a == b).Should().BeFalse();
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        var zero = Money.Zero("BRL");

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("BRL");
    }
}
