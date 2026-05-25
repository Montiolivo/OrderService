using MediatR;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;

namespace OrderService.Application.Orders.Commands.ConfirmOrder;

public sealed class ConfirmOrderHandler : IRequestHandler<ConfirmOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        Order order = await _orderRepository.GetByIdWithItemsAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(order), command.OrderId);

        // Confirm() retorna false se já estava confirmado → idempotência
        var transitioned = order.Confirm();

        if (transitioned)
        {
            // Só baixa estoque se houve transição real (Placed → Confirmed)
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

            foreach (var item in order.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.ReserveStock(item.Quantity); // lança InsufficientStockException se falhar
                _productRepository.Update(product);
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return OrderMapper.ToDto(order);
    }
}
