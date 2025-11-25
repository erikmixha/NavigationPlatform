using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Journey.API.Controllers;
using Journey.API.DTOs;
using Journey.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Journey.IntegrationTests;

[Trait("Category", "Integration")]
public class JourneyApiIntegrationTests : IClassFixture<JourneyApiTestFixture>, IAsyncLifetime
{
    private readonly JourneyApiTestFixture _fixture;

    public JourneyApiIntegrationTests(JourneyApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task POST_Journeys_ShouldCreateJourney_AndReturn201()
    {
        var client = _fixture.CreateClientWithUser("test-user-id");
        var request = new CreateJourneyRequest
        {
            StartLocation = "New York, NY",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Boston, MA",
            ArrivalTime = DateTime.UtcNow.AddHours(2),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 350.50m
        };

        var response = await client.PostAsJsonAsync("/api/journeys", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_Journeys_ById_ShouldReturn200_WhenJourneyExists()
    {
        var client = _fixture.CreateClientWithUser("test-user-id");
        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Location A",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Location B",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var response = await client.GetAsync($"/api/journeys/{journeyId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var journey = await response.Content.ReadFromJsonAsync<Journey.Application.DTOs.JourneyDto>();
        journey.Should().NotBeNull();
        journey!.Id.Should().Be(journeyId);
        journey.StartLocation.Should().Be("Location A");
        journey.DistanceKm.Should().Be(100.00m);
    }

    [Fact]
    public async Task GET_Journeys_ById_ShouldReturn403_WhenNotOwnerOrShared()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var otherUserClient = _fixture.CreateClientWithUser("other-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Private Journey",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Destination",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var response = await otherUserClient.GetAsync($"/api/journeys/{journeyId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_Journeys_ById_ShouldReturn200_WhenSharedWithUser()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var sharedUserClient = _fixture.CreateClientWithUser("shared-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Shared Journey",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Destination",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var shareRequest = new ShareJourneyRequest
        {
            SharedWithUserIds = new List<string> { "shared-user-id" }
        };
        await ownerClient.PostAsJsonAsync($"/api/journeys/{journeyId}/share", shareRequest);

        var response = await sharedUserClient.GetAsync($"/api/journeys/{journeyId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Journeys_ShouldReturnPagedList_WithOwnAndSharedJourneys()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var sharedUserClient = _fixture.CreateClientWithUser("shared-user-id");

        var ownJourney = new CreateJourneyRequest
        {
            StartLocation = "Own Journey",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Destination",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 50.00m
        };
        var ownResponse = await ownerClient.PostAsJsonAsync("/api/journeys", ownJourney);
        var ownCreated = await ownResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();

        var sharedJourney = new CreateJourneyRequest
        {
            StartLocation = "Shared Journey",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Destination",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 75.00m
        };
        var sharedResponse = await ownerClient.PostAsJsonAsync("/api/journeys", sharedJourney);
        var sharedCreated = await sharedResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();

        var shareRequest = new ShareJourneyRequest
        {
            SharedWithUserIds = new List<string> { "shared-user-id" }
        };
        await ownerClient.PostAsJsonAsync($"/api/journeys/{sharedCreated!.Id}/share", shareRequest);

        var response = await sharedUserClient.GetAsync("/api/journeys?Page=1&PageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedResult = await response.Content.ReadFromJsonAsync<Journey.Application.DTOs.PagedResult<Journey.Application.DTOs.JourneyDto>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().Contain(j => j.Id == sharedCreated.Id);
        pagedResult.Items.Should().NotContain(j => j.Id == ownCreated!.Id);
    }

    [Fact]
    public async Task PUT_Journeys_ById_ShouldReturn403_WhenNotOwner()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var otherUserClient = _fixture.CreateClientWithUser("other-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Original Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Original End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var updateRequest = new UpdateJourneyRequest
        {
            StartLocation = "Updated Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Updated End",
            ArrivalTime = DateTime.UtcNow.AddHours(2),
            TransportType = TransportType.Cargo.ToString(),
            DistanceKm = 200.00m
        };

        var response = await otherUserClient.PutAsJsonAsync($"/api/journeys/{journeyId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PUT_Journeys_ById_ShouldUpdateJourney_AndReturn204_WhenOwner()
    {
        var client = _fixture.CreateClientWithUser("owner-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Original Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Original End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var updateRequest = new UpdateJourneyRequest
        {
            StartLocation = "Updated Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Updated End",
            ArrivalTime = DateTime.UtcNow.AddHours(2),
            TransportType = TransportType.Cargo.ToString(),
            DistanceKm = 200.00m
        };

        var response = await client.PutAsJsonAsync($"/api/journeys/{journeyId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/journeys/{journeyId}");
        var journey = await getResponse.Content.ReadFromJsonAsync<Journey.Application.DTOs.JourneyDto>();
        journey!.StartLocation.Should().Be("Updated Start");
        journey.DistanceKm.Should().Be(200.00m);
    }

    [Fact]
    public async Task DELETE_Journeys_ById_ShouldReturn403_WhenNotOwner()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var otherUserClient = _fixture.CreateClientWithUser("other-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "To Delete",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Destination",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 50.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var response = await otherUserClient.DeleteAsync($"/api/journeys/{journeyId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DELETE_Journeys_ById_ShouldDeleteJourney_AndReturn204_WhenOwner()
    {
        var client = _fixture.CreateClientWithUser("owner-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "To Delete",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Destination",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 50.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var response = await client.DeleteAsync($"/api/journeys/{journeyId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/journeys/{journeyId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Journeys_Share_ShouldShareWithUsers_AndCreateAudit()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var shareRequest = new ShareJourneyRequest
        {
            SharedWithUserIds = new List<string> { "user1", "user2" }
        };

        var response = await ownerClient.PostAsJsonAsync($"/api/journeys/{journeyId}/share", shareRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Journey.Infrastructure.Persistence.JourneyDbContext>();
        var audits = await context.ShareAudits
            .Where(sa => sa.JourneyId == journeyId && sa.Action == "Shared")
            .ToListAsync();
        audits.Should().HaveCount(2);
        audits.Should().Contain(a => a.TargetUserId == "user1");
        audits.Should().Contain(a => a.TargetUserId == "user2");
        audits.All(a => a.PerformedByUserId == "owner-user-id").Should().BeTrue();
    }

    [Fact]
    public async Task DELETE_Journeys_Share_ShouldUnshareFromUser_AndCreateAudit()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var shareRequest = new ShareJourneyRequest
        {
            SharedWithUserIds = new List<string> { "user1" }
        };
        await ownerClient.PostAsJsonAsync($"/api/journeys/{journeyId}/share", shareRequest);

        var response = await ownerClient.DeleteAsync($"/api/journeys/{journeyId}/share/user1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Journey.Infrastructure.Persistence.JourneyDbContext>();
        var unshareAudit = await context.ShareAudits
            .FirstOrDefaultAsync(sa => sa.JourneyId == journeyId && sa.Action == "Unshared" && sa.TargetUserId == "user1");
        unshareAudit.Should().NotBeNull();
        unshareAudit!.PerformedByUserId.Should().Be("owner-user-id");
    }

    [Fact]
    public async Task POST_Journeys_Share_ShouldReturn403_WhenNotOwner()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var otherUserClient = _fixture.CreateClientWithUser("other-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var shareRequest = new ShareJourneyRequest
        {
            SharedWithUserIds = new List<string> { "user1" }
        };

        var response = await otherUserClient.PostAsJsonAsync($"/api/journeys/{journeyId}/share", shareRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task POST_Journeys_PublicLink_ShouldGenerateLink_AndCreateAudit()
    {
        var client = _fixture.CreateClientWithUser("owner-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var response = await client.PostAsync($"/api/journeys/{journeyId}/public-link", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PublicLinkResponse>();
        result.Should().NotBeNull();
        result!.Url.Should().NotBeNullOrEmpty();
        result.Token.Should().NotBeNullOrEmpty();

        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Journey.Infrastructure.Persistence.JourneyDbContext>();
        var audit = await context.ShareAudits
            .FirstOrDefaultAsync(sa => sa.JourneyId == journeyId && sa.Action == "PublicLinkGenerated");
        audit.Should().NotBeNull();
        audit!.PerformedByUserId.Should().Be("owner-user-id");
    }

    [Fact]
    public async Task GET_Journeys_PublicLink_ShouldReturnJourney_WhenNotRevoked()
    {
        var client = _fixture.CreateClientWithUser("owner-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Public Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "Public End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var linkResponse = await client.PostAsync($"/api/journeys/{journeyId}/public-link", null);
        var link = await linkResponse.Content.ReadFromJsonAsync<PublicLinkResponse>();
        var token = link!.Token;

        var anonymousClient = _fixture.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/journeys/public/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var journey = await response.Content.ReadFromJsonAsync<Journey.Application.DTOs.JourneyDto>();
        journey.Should().NotBeNull();
        journey!.StartLocation.Should().Be("Public Start");
    }

    [Fact]
    public async Task GET_Journeys_PublicLink_ShouldReturn410_WhenRevoked()
    {
        var client = _fixture.CreateClientWithUser("owner-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var linkResponse = await client.PostAsync($"/api/journeys/{journeyId}/public-link", null);
        var link = await linkResponse.Content.ReadFromJsonAsync<PublicLinkResponse>();
        var token = link!.Token;

        await client.DeleteAsync($"/api/journeys/{journeyId}/public-link");

        var anonymousClient = _fixture.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/journeys/public/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task POST_Journeys_Favorite_ShouldAddFavorite_WhenOwner()
    {
        var client = _fixture.CreateClientWithUser("owner-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await client.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var response = await client.PostAsync($"/api/journeys/{journeyId}/favorite", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Journeys_Favorite_ShouldAddFavorite_WhenSharedWithUser()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var sharedUserClient = _fixture.CreateClientWithUser("shared-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var shareRequest = new ShareJourneyRequest
        {
            SharedWithUserIds = new List<string> { "shared-user-id" }
        };
        await ownerClient.PostAsJsonAsync($"/api/journeys/{journeyId}/share", shareRequest);

        var response = await sharedUserClient.PostAsync($"/api/journeys/{journeyId}/favorite", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Journeys_Favorite_ShouldReturn403_WhenNoAccess()
    {
        var ownerClient = _fixture.CreateClientWithUser("owner-user-id");
        var otherUserClient = _fixture.CreateClientWithUser("other-user-id");

        var createRequest = new CreateJourneyRequest
        {
            StartLocation = "Start",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(1),
            TransportType = TransportType.Commercial.ToString(),
            DistanceKm = 100.00m
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/api/journeys", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJourneyResponse>();
        var journeyId = created!.Id;

        var response = await otherUserClient.PostAsync($"/api/journeys/{journeyId}/favorite", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task POST_Journeys_WithInvalidData_ShouldReturn400()
    {
        var client = _fixture.CreateClientWithUser("test-user-id");
        var invalidRequest = new CreateJourneyRequest
        {
            StartLocation = "",
            StartTime = DateTime.UtcNow,
            ArrivalLocation = "End",
            ArrivalTime = DateTime.UtcNow.AddHours(-1),
            TransportType = "InvalidType",
            DistanceKm = -10.00m
        };

        var response = await client.PostAsJsonAsync("/api/journeys", invalidRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
