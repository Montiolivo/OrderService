using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace OrderService.IntegrationTests.Infrastructure;

/// <summary>
/// Classe base para todos os integration tests.
/// Fornece HttpClient autenticado e helpers de request/response.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<OrderServiceWebAppFactory>
{
    protected readonly HttpClient Client;
    protected readonly OrderServiceWebAppFactory Factory;

    protected static readonly Guid NotebookId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    protected static readonly Guid MouseId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    protected static readonly Guid KeyboardId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(OrderServiceWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected HttpClient CreateUnauthenticatedClient()
    => Factory.CreateClient();

    protected async Task AuthenticateAsync()
    {
        var response = await Client.PostAsJsonAsync("/auth/token", new
        {
            username = "admin",
            password = "admin"
        });

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = content.GetProperty("accessToken").GetString()!;

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
    }
}
