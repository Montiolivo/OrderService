using FluentAssertions;
using NSubstitute;
using OrderService.Application.Interfaces;
using OrderService.Application.Orders.Commands.ConfirmOrder;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using Xunit;

namespace OrderService.UnitTests.Application.Orders;

public class ConfirmOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ConfirmOrderHandler _handler;

    private static readonly Guid ProductId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public ConfirmOrderHandlerTests()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _productRepository = Substitute.For<IProductRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new ConfirmOrderHandler(_orderRepository, _productRepository, _unitOfWork);
    }

    private static Order MakePlacedOrder()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");
        order.AddItem(ProductId, "Notebook", 100m, "BRL", 2);
        return order;
    }

    private static Product MakeProduct(int stock = 10) =>
        new(ProductId, "Notebook", 100m, "BRL", stock);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldConfirmOrder_WhenPlaced()
    {
        var order = MakePlacedOrder();
        var product = MakeProduct(stock: 10);

        _orderRepository.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product }.AsReadOnly());

        var result = await _handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task Handle_ShouldReserveStock_WhenConfirming()
    {
        var order = MakePlacedOrder();
        var product = MakeProduct(stock: 10);

        _orderRepository.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product }.AsReadOnly());

        await _handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        product.AvailableQuantity.Should().Be(8);
        _productRepository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Idempotência ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenAlreadyConfirmed()
    {
        var order = MakePlacedOrder();
        order.Confirm();

        _orderRepository.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be("Confirmed");

        await _productRepository.DidNotReceive()
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Sad paths ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotFound()
    {
        _orderRepository.GetByIdWithItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var act = async () =>
            await _handler.Handle(new ConfirmOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderIsCanceled()
    {
        var order = MakePlacedOrder();
        order.Cancel();

        _orderRepository.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var act = async () =>
            await _handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*cannot be confirmed*");
    }
}
