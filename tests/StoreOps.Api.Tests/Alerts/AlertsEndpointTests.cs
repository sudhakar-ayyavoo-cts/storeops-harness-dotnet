using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StoreOps.Api.Contracts.Alerts;
using StoreOps.Domain.Alerts;

namespace StoreOps.Api.Tests.Alerts;

public sealed class AlertsEndpointTests : IClassFixture<StoreOpsWebFactory>
{
    private readonly HttpClient _client;

    public AlertsEndpointTests(StoreOpsWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Alerts_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/alerts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task Patch_AlertStatus_WhenNotFound_Returns404()
    {
        var unknownId = Guid.NewGuid();
        var dto = new UpdateAlertStatusRequestDto { Status = NotificationStatus.Read };

        var response = await _client.PatchAsJsonAsync($"/api/alerts/{unknownId}/status", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ALERT_NOT_FOUND");
    }
}
