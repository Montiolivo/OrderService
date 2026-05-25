using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Requests;
using OrderService.Application.DTOs;
using OrderService.Application.Orders.Commands.CancelOrder;
using OrderService.Application.Orders.Commands.ConfirmOrder;
using OrderService.Application.Orders.Commands.CreateOrder;
using OrderService.Application.Orders.Queries.GetOrderById;
using OrderService.Application.Orders.Queries.ListOrders;
using OrderService.Domain.Enums;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Cria um novo pedido.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            CustomerId: request.CustomerId,
            Currency: request.Currency,
            Items: request.Items
                .Select(i => new CreateOrderItemCommand(i.ProductId, i.Quantity))
                .ToList()
        );

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Confirma um pedido (idempotente).
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConfirmOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancela um pedido (idempotente).
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retorna um pedido pelo Id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lista pedidos com filtros e paginação.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? customerId,
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Garante limites razoáveis de paginação
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new ListOrdersQuery(customerId, status, from, to, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
