using MediatR;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;

namespace OrderService.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // 1. Carrega todos os produtos de uma vez (evita N+1)
        var productIds = command.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        // 2. Valida que todos os produtos existem
        var missingProductId = productIds.FirstOrDefault(id => products.All(p => p.Id != id));
        if (missingProductId != Guid.Empty)
            throw new NotFoundException(nameof(Product), missingProductId);

        // 3. Valida estoque antes de criar o pedido (fail fast)
        foreach (var item in command.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            if (!product.HasSufficientStock(item.Quantity))
                throw new InsufficientStockException(item.ProductId, item.Quantity, product.AvailableQuantity);
        }

        // 4. Cria o aggregate
        var order = Order.Create(command.CustomerId, command.Currency);

        foreach (var item in command.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            order.AddItem(
                productId: product.Id,
                productName: product.Name,
                unitPrice: product.UnitPrice,
                currency: product.Currency,
                quantity: item.Quantity
            );
        }

        // 5. Persiste
        _orderRepository.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderMapper.ToDto(order);
    }
}
