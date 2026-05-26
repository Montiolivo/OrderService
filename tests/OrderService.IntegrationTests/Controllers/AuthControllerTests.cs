using FluentAssertions;
using OrderService.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OrderService.IntegrationTests.Controllers;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(OrderServiceWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task PostToken_ShouldReturnToken_WhenValidCredentials()
    {
        var response = await Client.PostAsJsonAsync("/auth/token", new
        {
            username = "admin",
            password = "admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        content.GetProperty("tokenType").GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task PostToken_ShouldReturn401_WhenInvalidCredentials()
    {
        var response = await Client.PostAsJsonAsync("/auth/token", new
        {
            username = "wrong",
            password = "wrong"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OrderEndpoints_ShouldReturn401_WhenNoToken()
    {
        var response = await Client.GetAsync("/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
