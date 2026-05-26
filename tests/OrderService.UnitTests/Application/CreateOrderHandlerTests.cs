using FluentAssertions;
using NSubstitute;
using OrderService.Application.Interfaces;
using OrderService.Application.Orders.Commands.CreateOrder;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using Xunit;

namespace OrderService.UnitTests.Application.Orders;

public class CreateOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateOrderHandler _handler;

    private static readonly Guid ProductId1 = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ProductId2 = Guid.Parse("10000000-0000-0000-0000-000000000002");

    public CreateOrderHandlerTests()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _productRepository = Substitute.For<IProductRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new CreateOrderHandler(_orderRepository, _productRepository, _unitOfWork);
    }

    private static Product MakeProduct(Guid id, string name, decimal price, int stock) =>
        new(id, name, price, "BRL", stock);

    private static CreateOrderCommand MakeCommand(params (Guid productId, int qty)[] items) =>
        new(
            CustomerId: Guid.NewGuid(),
            Currency: "BRL",
            Items: items.Select(i => new CreateOrderItemCommand(i.productId, i.qty)).ToList()
        );

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenValidCommand()
    {
        var product = MakeProduct(ProductId1, "Notebook Pro 15", 4999.99m, 50);
        var command = MakeCommand((ProductId1, 2));

        _productRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product }.AsReadOnly());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(9999.98m);
        result.Status.Should().Be("Placed");
        result.Currency.Should().Be("BRL");
    }

    [Fact]
    public async Task Handle_ShouldPersistOrder_CallingAddAndSaveChanges()
    {
        var product = MakeProduct(ProductId1, "Notebook", 100m, 10);
        var command = MakeCommand((ProductId1, 1));

        _productRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product }.AsReadOnly());

        await _handler.Handle(command, CancellationToken.None);

        _orderRepository.Received(1).Add(Arg.Any<Order>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_WithMultipleItems()
    {
        var p1 = MakeProduct(ProductId1, "Notebook", 1000m, 10);
        var p2 = MakeProduct(ProductId2, "Mouse", 100m, 20);
        var command = MakeCommand((ProductId1, 1), (ProductId2, 3));

        _productRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { p1, p2 }.AsReadOnly());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(1300m);
    }

    // ── Sad paths ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenProductNotFound()
    {
        var unknownId = Guid.NewGuid();
        var command = MakeCommand((unknownId, 1));

        _productRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>().AsReadOnly());

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowInsufficientStockException_WhenStockIsInsufficient()
    {
        var product = MakeProduct(ProductId1, "Notebook", 100m, stock: 1);
        var command = MakeCommand((ProductId1, 5));

        _productRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product }.AsReadOnly());

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientStockException>()
            .WithMessage($"*{ProductId1}*");
    }

    [Fact]
    public async Task Handle_ShouldNotSaveChanges_WhenStockIsInsufficient()
    {
        var product = MakeProduct(ProductId1, "Notebook", 100m, stock: 1);
        var command = MakeCommand((ProductId1, 5));

        _productRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product }.AsReadOnly());

        try { await _handler.Handle(command, CancellationToken.None); } catch { }

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
