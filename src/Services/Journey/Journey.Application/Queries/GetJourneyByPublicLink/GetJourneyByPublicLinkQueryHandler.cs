using Journey.Application.Interfaces;
using Journey.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Common.Result;

namespace Journey.Application.Queries.GetJourneyByPublicLink;

/// <summary>
/// Handler for getting a journey by its public link token.
/// </summary>
public sealed class GetJourneyByPublicLinkQueryHandler : IRequestHandler<GetJourneyByPublicLinkQuery, Result<JourneyDto>>
{
    private readonly IJourneyRepository _repository;
    private readonly ILogger<GetJourneyByPublicLinkQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetJourneyByPublicLinkQueryHandler"/> class.
    /// </summary>
    public GetJourneyByPublicLinkQueryHandler(
        IJourneyRepository repository,
        ILogger<GetJourneyByPublicLinkQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<JourneyDto>> Handle(GetJourneyByPublicLinkQuery request, CancellationToken cancellationToken)
    {
        var publicLink = await _repository.GetPublicLinkByTokenAsync(request.Token, cancellationToken);
        if (publicLink is null)
        {
            return Result.Failure<JourneyDto>(new Error("Journey.PublicLinkNotFound", "Public link not found"));
        }

        if (publicLink.IsRevoked)
        {
            return Result.Failure<JourneyDto>(new Error("Journey.PublicLinkRevoked", "Public link has been revoked"));
        }

        var journey = publicLink.Journey;

        var journeyDto = new JourneyDto
        {
            Id = journey.Id,
            UserId = journey.UserId,
            StartLocation = journey.StartLocation,
            StartTime = journey.StartTime,
            ArrivalLocation = journey.ArrivalLocation,
            ArrivalTime = journey.ArrivalTime,
            TransportType = journey.TransportType.ToString(),
            DistanceKm = journey.DistanceKm.Value,
            IsFavorite = false,
            IsShared = false
        };

        _logger.LogInformation("Journey {JourneyId} accessed via public link", journey.Id);

        return Result.Success(journeyDto);
    }
}
