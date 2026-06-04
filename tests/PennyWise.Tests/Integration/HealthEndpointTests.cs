using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PennyWise.Tests.Integration;

/// <summary>
/// Integration tests for the Health endpoint.
/// </summary>
public class HealthEndpointTests : IClassFixture<PennyWiseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(PennyWiseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Endpoint_Returns_OK()
    {
        var response = await _client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_Endpoint_Returns_Valid_Payload()
    {
        var response = await _client.GetAsync("/api/health");
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();

        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Service.Should().Be("PennyWise API");
    }

    private record HealthResponse(string Status, string Service, string Version, DateTime Timestamp);
}
