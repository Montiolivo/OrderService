using FluentAssertions;
using OrderService.Application.DTOs;
using OrderService.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrderService.IntegrationTests.Controllers;

public class OrdersControllerTests : IntegrationTestBase
{
    public OrdersControllerTests(OrderServiceWebAppFactory factory) : base(factory) { }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private object DefaultCreatePayload(Guid? productId = null, int qty = 1) => new
    {
        customerId = Guid.NewGuid(),
        currency = "BRL",
        items = new[]
        {
            new { productId = productId ?? NotebookId, quantity = qty }
        }
    };

    private async Task<OrderDto> CreateOrderAsync(object? payload = null)
    {
        var response = await Client.PostAsJsonAsync("/orders", payload ?? DefaultCreatePayload());
        response.EnsureSuccessStatusCode();
        return (await ReadAsync<OrderDto>(response))!;
    }

    // ── POST /orders ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrder_ShouldReturn201_WithValidPayload()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/orders", DefaultCreatePayload(qty: 2));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var order = await ReadAsync<OrderDto>(response);
        order.Should().NotBeNull();
        order.Status.Should().Be("Placed");
        order.Currency.Should().Be("BRL");
        order.Items.Should().HaveCount(1);
        order.Total.Should().Be(9999.98m);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn400_WhenNoItems()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/orders", new
        {
            customerId = Guid.NewGuid(),
            currency = "BRL",
            items = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn400_WhenQuantityIsZero()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/orders", DefaultCreatePayload(qty: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn404_WhenProductNotFound()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/orders",
            DefaultCreatePayload(productId: Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn409_WhenInsufficientStock()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsJsonAsync("/orders",
            DefaultCreatePayload(qty: 99999));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── GET /orders/{id} ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ShouldReturnOrder_WhenExists()
    {
        await AuthenticateAsync();
        var created = await CreateOrderAsync();

        var response = await Client.GetAsync($"/orders/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await ReadAsync<OrderDto>(response);
        order.Id.Should().Be(created.Id);
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenNotFound()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync($"/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /orders/{id}/confirm ─────────────────────────────────────────────

    [Fact]
    public async Task ConfirmOrder_ShouldReturn200_WithConfirmedStatus()
    {
        await AuthenticateAsync();
        var order = await CreateOrderAsync();

        var response = await Client.PostAsync($"/orders/{order.Id}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmed = await ReadAsync<OrderDto>(response);
        confirmed.Status.Should().Be("Confirmed");
        confirmed.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmOrder_ShouldBeIdempotent_WhenCalledTwice()
    {
        await AuthenticateAsync();
        var order = await CreateOrderAsync();

        await Client.PostAsync($"/orders/{order.Id}/confirm", null);
        var response = await Client.PostAsync($"/orders/{order.Id}/confirm", null); // segunda vez

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadAsync<OrderDto>(response);
        result.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task ConfirmOrder_ShouldReturn404_WhenOrderNotFound()
    {
        await AuthenticateAsync();

        var response = await Client.PostAsync($"/orders/{Guid.NewGuid()}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /orders/{id}/cancel ──────────────────────────────────────────────

    [Fact]
    public async Task CancelOrder_ShouldReturn200_FromPlaced()
    {
        await AuthenticateAsync();
        var order = await CreateOrderAsync();

        var response = await Client.PostAsync($"/orders/{order.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var canceled = await ReadAsync<OrderDto>(response);
        canceled.Status.Should().Be("Canceled");
        canceled.CanceledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelOrder_ShouldReturn200_FromConfirmed()
    {
        await AuthenticateAsync();
        var order = await CreateOrderAsync();
        await Client.PostAsync($"/orders/{order.Id}/confirm", null);

        var response = await Client.PostAsync($"/orders/{order.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var canceled = await ReadAsync<OrderDto>(response);
        canceled.Status.Should().Be("Canceled");
    }

    [Fact]
    public async Task CancelOrder_ShouldBeIdempotent_WhenCalledTwice()
    {
        await AuthenticateAsync();
        var order = await CreateOrderAsync();

        await Client.PostAsync($"/orders/{order.Id}/cancel", null);
        var response = await Client.PostAsync($"/orders/{order.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadAsync<OrderDto>(response);
        result.Status.Should().Be("Canceled");
    }

    // ── GET /orders ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListOrders_ShouldReturnPagedResult()
    {
        await AuthenticateAsync();
        await CreateOrderAsync();
        await CreateOrderAsync();

        var response = await Client.GetAsync("/orders?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadAsync<PagedResult<OrderDto>>(response);
        result.Should().NotBeNull();
        result.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListOrders_ShouldFilterByCustomerId()
    {
        await AuthenticateAsync();
        var customerId = Guid.NewGuid();

        await Client.PostAsJsonAsync("/orders", new
        {
            customerId,
            currency = "BRL",
            items = new[] { new { productId = NotebookId, quantity = 1 } }
        });

        var response = await Client.GetAsync($"/orders?customerId={customerId}");
        var result = await ReadAsync<PagedResult<OrderDto>>(response);

        result.Items.Should().AllSatisfy(o => o.CustomerId.Should().Be(customerId));
    }
}
