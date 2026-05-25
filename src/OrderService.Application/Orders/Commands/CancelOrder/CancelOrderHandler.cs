using MediatR;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;

namespace OrderService.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.OrderId);

        // CancelResult informa se houve transição e de qual estado
        var result = order.Cancel();

        switch (result)
        {
            case CancelResult.AlreadyCanceled:
                // Idempotente: retorna estado atual sem persistir
                return OrderMapper.ToDto(order);

            case CancelResult.CanceledFromConfirmed:
                // Estoque foi baixado na confirmação — precisa liberar
                var productIds = order.Items.Select(i => i.ProductId).ToList();
                var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

                foreach (var item in order.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    product.ReleaseStock(item.Quantity);
                    _productRepository.Update(product);
                }

                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                break;

            case CancelResult.CanceledFromPlaced:
                // Estoque nunca foi baixado — só persiste o status
                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                break;
        }

        return OrderMapper.ToDto(order);
    }
}
