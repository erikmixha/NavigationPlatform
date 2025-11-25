using Journey.Application.Queries.Admin.GetStatistics;

namespace Journey.Application.Interfaces;

/// <summary>
/// Repository interface for journey data access operations.
/// </summary>
public interface IJourneyRepository
{
    /// <summary>
    /// Gets a journey by its identifier.
    /// </summary>
    Task<Domain.Entities.Journey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all journeys for a specific user.
    /// </summary>
    Task<List<Domain.Entities.Journey>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated journeys for a user with optional date filtering.
    /// </summary>
    Task<(List<Domain.Entities.Journey> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string userId,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can access a specific journey.
    /// </summary>
    Task<bool> CanAccessJourneyAsync(Guid journeyId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets journeys for a user on a specific date.
    /// </summary>
    Task<List<Domain.Entities.Journey>> GetByUserIdAndDateAsync(string userId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new journey.
    /// </summary>
    Task AddAsync(Domain.Entities.Journey journey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing journey.
    /// </summary>
    void Update(Domain.Entities.Journey journey);

    /// <summary>
    /// Removes a journey.
    /// </summary>
    void Remove(Domain.Entities.Journey journey);

    /// <summary>
    /// Gets a journey share by journey ID and user ID.
    /// </summary>
    Task<Domain.Entities.JourneyShare?> GetShareAsync(Guid journeyId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all user IDs that a journey is shared with.
    /// </summary>
    Task<List<string>> GetSharedWithUserIdsAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a journey share.
    /// </summary>
    Task AddShareAsync(Domain.Entities.JourneyShare share, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a journey share.
    /// </summary>
    Task RemoveShareAsync(Domain.Entities.JourneyShare share, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a favorite relationship between a journey and user.
    /// </summary>
    Task<Domain.Entities.JourneyFavorite?> GetFavoriteAsync(Guid journeyId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all favorite journey IDs for a user.
    /// </summary>
    Task<List<Guid>> GetFavoriteJourneyIdsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all user IDs that favorited a journey.
    /// </summary>
    Task<List<string>> GetFavoritingUserIdsAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a favorite relationship.
    /// </summary>
    Task AddFavoriteAsync(Domain.Entities.JourneyFavorite favorite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a favorite relationship.
    /// </summary>
    Task RemoveFavoriteAsync(Domain.Entities.JourneyFavorite favorite, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a public link by token.
    /// </summary>
    Task<Domain.Entities.PublicLink?> GetPublicLinkByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active public link for a journey.
    /// </summary>
    Task<Domain.Entities.PublicLink?> GetActivePublicLinkByJourneyIdAsync(Guid journeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a public link.
    /// </summary>
    Task AddPublicLinkAsync(Domain.Entities.PublicLink link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a public link.
    /// </summary>
    void UpdatePublicLink(Domain.Entities.PublicLink link);

    /// <summary>
    /// Adds a share audit record.
    /// </summary>
    Task AddShareAuditAsync(Domain.Entities.ShareAudit audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of journeys matching the filters.
    /// </summary>
    Task<int> GetTotalCountAsync(
        string? userId = null,
        Domain.Enums.TransportType? transportType = null,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        DateTime? arrivalDateFrom = null,
        DateTime? arrivalDateTo = null,
        decimal? minDistance = null,
        decimal? maxDistance = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all journeys with pagination and filtering for admin operations.
    /// </summary>
    Task<List<Domain.Entities.Journey>> GetAllPagedAsync(
        int page,
        int pageSize,
        string? userId = null,
        Domain.Enums.TransportType? transportType = null,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        DateTime? arrivalDateFrom = null,
        DateTime? arrivalDateTo = null,
        decimal? minDistance = null,
        decimal? maxDistance = null,
        string? orderBy = null,
        string? direction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for all journeys.
    /// </summary>
    Task<StatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets monthly distance statistics.
    /// </summary>
    Task<List<MonthlyDistanceDto>> GetMonthlyDistanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a monthly distance read model for a user and month.
    /// </summary>
    Task<Domain.Entities.MonthlyDistanceReadModel?> GetMonthlyDistanceReadModelAsync(string userId, int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a monthly distance read model.
    /// </summary>
    Task AddMonthlyDistanceReadModelAsync(Domain.Entities.MonthlyDistanceReadModel readModel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all monthly distance read models.
    /// </summary>
    Task<List<Domain.Entities.MonthlyDistanceReadModel>> GetMonthlyDistanceReadModelsAsync(CancellationToken cancellationToken = default);
}

