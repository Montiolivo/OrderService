using FluentAssertions;
using NSubstitute;
using OrderService.Application.Interfaces;
using OrderService.Application.Orders.Commands.CancelOrder;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using Xunit;

namespace OrderService.UnitTests.Application.Orders;

public class CancelOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CancelOrderHandler _handler;

    private static readonly Guid ProductId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    public CancelOrderHandlerTests()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _productRepository = Substitute.For<IProductRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _handler = new CancelOrderHandler(_orderRepository, _productRepository, _unitOfWork);
    }

    private static Order MakePlacedOrder()
    {
        var order = Order.Create(Guid.NewGuid(), "BRL");
        order.AddItem(ProductId, "Notebook", 100m, "BRL", 2);
        return order;
    }

    private static Order MakeConfirmedOrder()
    {
        var order = MakePlacedOrder();
        order.Confirm();
        return order;
    }

    private static Product MakeProduct(int stock = 8) =>
        new(ProductId, "Notebook", 100m, "BRL", stock);

    // ── Cancel from Placed ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldCancel_WhenPlaced()
    {
        var order = MakePlacedOrder();
        _orderRepository.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be("Canceled");
    }

    [Fact]
    public async Task Handle_ShouldNotReleaseStock_WhenCanceledFromPlaced()
    {
        var order = MakePlacedOrder();
        _orderRepository.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        await _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        await _productRepository.DidNotReceive()
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }

    // ── Cancel from Confirmed ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReleaseStock_WhenCanceledFromConfirmed()
    {
        var order = MakeConfirmedOrder();
        var product = MakeProduct(stock: 8); 

        _orderRepository.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product }.AsReadOnly());

        await _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        product.AvailableQuantity.Should().Be(10);
        _productRepository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Idempotência ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenAlreadyCanceled()
    {
        var order = MakePlacedOrder();
        order.Cancel();

        _orderRepository.GetByIdWithItemsAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be("Canceled");

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
            await _handler.Handle(new CancelOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
