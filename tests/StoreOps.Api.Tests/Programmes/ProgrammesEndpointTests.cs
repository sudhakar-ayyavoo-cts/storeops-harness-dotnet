using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StoreOps.Api.Contracts.Programmes;

namespace StoreOps.Api.Tests.Programmes;

public sealed class ProgrammesEndpointTests : IClassFixture<StoreOpsWebFactory>
{
    private readonly HttpClient _client;

    public ProgrammesEndpointTests(StoreOpsWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Programmes_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/programmes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ProgrammeDto>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_WithValidRequest_CreatesProgrammeAndReturnsCreated()
    {
        var storeId = Guid.NewGuid();
        var dto = new CreateProgrammeRequestDto
        {
            Name = "Summer Refit 2026",
            StoreId = storeId,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddMonths(3),
        };

        var response = await _client.PostAsJsonAsync("/api/programmes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ProgrammeDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Summer Refit 2026");
        body.StoreId.Should().Be(storeId);
        body.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Post_WithEmptyName_Returns422()
    {
        var dto = new CreateProgrammeRequestDto
        {
            Name = string.Empty,
            StoreId = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
        };

        var response = await _client.PostAsJsonAsync("/api/programmes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("PROGRAMME_VALIDATION_ERROR");
    }
}
