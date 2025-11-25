using Journey.Application.Interfaces;
using Journey.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Journey.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Excluded from code coverage: Infrastructure repository implementation.
/// Database operations are tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure repository. Tested via integration tests.")]
public sealed class JourneyRepository : IJourneyRepository
{
    private readonly JourneyDbContext _context;

    public JourneyRepository(JourneyDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Entities.Journey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<List<Domain.Entities.Journey>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Domain.Entities.Journey> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string userId,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Journeys
            .Where(j => j.UserId == userId || _context.JourneyShares.Any(js => js.JourneyId == j.Id && js.SharedWithUserId == userId));

        if (startDateFrom.HasValue)
        {
            query = query.Where(j => j.StartTime >= startDateFrom.Value);
        }

        if (startDateTo.HasValue)
        {
            query = query.Where(j => j.StartTime <= startDateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> CanAccessJourneyAsync(Guid journeyId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Journeys
            .AnyAsync(j => j.Id == journeyId && 
                (j.UserId == userId || _context.JourneyShares.Any(js => js.JourneyId == journeyId && js.SharedWithUserId == userId)), 
                cancellationToken);
    }

    public async Task<List<Domain.Entities.Journey>> GetByUserIdAndDateAsync(string userId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Journeys
            .Where(j => j.UserId == userId && j.StartTime >= startOfDay && j.StartTime < endOfDay)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Domain.Entities.Journey journey, CancellationToken cancellationToken = default)
    {
        await _context.Journeys.AddAsync(journey, cancellationToken);
    }

    public void Update(Domain.Entities.Journey journey)
    {
        _context.Journeys.Update(journey);
    }

    public void Remove(Domain.Entities.Journey journey)
    {
        _context.Journeys.Remove(journey);
    }

    public async Task<JourneyShare?> GetShareAsync(Guid journeyId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneyShares
            .FirstOrDefaultAsync(js => js.JourneyId == journeyId && js.SharedWithUserId == userId, cancellationToken);
    }

    public async Task<List<string>> GetSharedWithUserIdsAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneyShares
            .Where(js => js.JourneyId == journeyId)
            .Select(js => js.SharedWithUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddShareAsync(JourneyShare share, CancellationToken cancellationToken = default)
    {
        await _context.JourneyShares.AddAsync(share, cancellationToken);
    }

    public async Task RemoveShareAsync(JourneyShare share, CancellationToken cancellationToken = default)
    {
        _context.JourneyShares.Remove(share);
        await Task.CompletedTask;
    }

    public async Task<JourneyFavorite?> GetFavoriteAsync(Guid journeyId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneyFavorites
            .FirstOrDefaultAsync(jf => jf.JourneyId == journeyId && jf.UserId == userId, cancellationToken);
    }

    public async Task<List<Guid>> GetFavoriteJourneyIdsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneyFavorites
            .Where(jf => jf.UserId == userId)
            .Select(jf => jf.JourneyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetFavoritingUserIdsAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        return await _context.JourneyFavorites
            .Where(jf => jf.JourneyId == journeyId)
            .Select(jf => jf.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddFavoriteAsync(JourneyFavorite favorite, CancellationToken cancellationToken = default)
    {
        await _context.JourneyFavorites.AddAsync(favorite, cancellationToken);
    }

    public async Task RemoveFavoriteAsync(JourneyFavorite favorite, CancellationToken cancellationToken = default)
    {
        _context.JourneyFavorites.Remove(favorite);
        await Task.CompletedTask;
    }

    public async Task<PublicLink?> GetPublicLinkByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.PublicLinks
            .Include(pl => pl.Journey)
            .FirstOrDefaultAsync(pl => pl.Token == token, cancellationToken);
    }

    public async Task<PublicLink?> GetActivePublicLinkByJourneyIdAsync(Guid journeyId, CancellationToken cancellationToken = default)
    {
        return await _context.PublicLinks
            .FirstOrDefaultAsync(pl => pl.JourneyId == journeyId && !pl.IsRevoked, cancellationToken);
    }

    public async Task AddPublicLinkAsync(PublicLink link, CancellationToken cancellationToken = default)
    {
        await _context.PublicLinks.AddAsync(link, cancellationToken);
    }

    public void UpdatePublicLink(PublicLink link)
    {
        _context.PublicLinks.Update(link);
    }

    public async Task AddShareAuditAsync(ShareAudit audit, CancellationToken cancellationToken = default)
    {
        await _context.ShareAudits.AddAsync(audit, cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        string? userId = null,
        Domain.Enums.TransportType? transportType = null,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        DateTime? arrivalDateFrom = null,
        DateTime? arrivalDateTo = null,
        decimal? minDistance = null,
        decimal? maxDistance = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Journeys.AsQueryable();
        query = ApplyFilters(query, userId, transportType, startDateFrom, startDateTo, arrivalDateFrom, arrivalDateTo, minDistance, maxDistance);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Journey>> GetAllPagedAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = _context.Journeys.AsQueryable();
        query = ApplyFilters(query, userId, transportType, startDateFrom, startDateTo, arrivalDateFrom, arrivalDateTo, minDistance, maxDistance);
        query = ApplySorting(query, orderBy, direction);

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Domain.Entities.Journey> ApplyFilters(
        IQueryable<Domain.Entities.Journey> query,
        string? userId,
        Domain.Enums.TransportType? transportType,
        DateTime? startDateFrom,
        DateTime? startDateTo,
        DateTime? arrivalDateFrom,
        DateTime? arrivalDateTo,
        decimal? minDistance,
        decimal? maxDistance)
    {
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(j => j.UserId == userId);

        if (transportType.HasValue)
            query = query.Where(j => j.TransportType == transportType.Value);

        if (startDateFrom.HasValue)
            query = query.Where(j => j.StartTime >= startDateFrom.Value);

        if (startDateTo.HasValue)
            query = query.Where(j => j.StartTime <= startDateTo.Value);

        if (arrivalDateFrom.HasValue)
            query = query.Where(j => j.ArrivalTime >= arrivalDateFrom.Value);

        if (arrivalDateTo.HasValue)
            query = query.Where(j => j.ArrivalTime <= arrivalDateTo.Value);

        if (minDistance.HasValue)
            query = query.Where(j => j.DistanceKm.Value >= minDistance.Value);

        if (maxDistance.HasValue)
            query = query.Where(j => j.DistanceKm.Value <= maxDistance.Value);

        return query;
    }

    private IQueryable<Domain.Entities.Journey> ApplySorting(
        IQueryable<Domain.Entities.Journey> query,
        string? orderBy,
        string? direction)
    {
        var isDescending = direction?.ToLower() == "desc";

        return orderBy?.ToLower() switch
        {
            "starttime" => isDescending ? query.OrderByDescending(j => j.StartTime) : query.OrderBy(j => j.StartTime),
            "arrivaltime" => isDescending ? query.OrderByDescending(j => j.ArrivalTime) : query.OrderBy(j => j.ArrivalTime),
            "distance" => isDescending ? query.OrderByDescending(j => j.DistanceKm.Value) : query.OrderBy(j => j.DistanceKm.Value),
            "transporttype" => isDescending ? query.OrderByDescending(j => j.TransportType) : query.OrderBy(j => j.TransportType),
            _ => query.OrderByDescending(j => j.StartTime)
        };
    }

    public async Task<Journey.Application.Queries.Admin.GetStatistics.StatisticsDto> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var allJourneys = await _context.Journeys.ToListAsync(cancellationToken);

        var totalJourneys = allJourneys.Count;
        var totalUsers = allJourneys.Select(j => j.UserId).Distinct().Count();
        var totalDistance = allJourneys.Sum(j => j.DistanceKm.Value);
        var averageDistance = totalJourneys > 0 ? totalDistance / totalJourneys : 0;

        var journeysByTransportType = allJourneys
            .GroupBy(j => j.TransportType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new Journey.Application.Queries.Admin.GetStatistics.StatisticsDto
        {
            TotalJourneys = totalJourneys,
            TotalUsers = totalUsers,
            TotalDistanceKm = totalDistance,
            AverageDistanceKm = averageDistance,
            JourneysByTransportType = journeysByTransportType,
            GeneratedOnUtc = DateTime.UtcNow
        };
    }

    public async Task<List<Journey.Application.Queries.Admin.GetStatistics.MonthlyDistanceDto>> GetMonthlyDistanceAsync(
        CancellationToken cancellationToken = default)
    {
        var readModels = await _context.MonthlyDistanceReadModels
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ThenBy(m => m.UserId)
            .Take(12)
            .ToListAsync(cancellationToken);

        return readModels
            .Select(m => new Journey.Application.Queries.Admin.GetStatistics.MonthlyDistanceDto
            {
                UserId = m.UserId,
                Year = m.Year,
                Month = m.Month,
                TotalDistanceKm = m.TotalDistanceKm
            })
            .ToList();
    }

    public async Task<Domain.Entities.MonthlyDistanceReadModel?> GetMonthlyDistanceReadModelAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await _context.MonthlyDistanceReadModels
            .FirstOrDefaultAsync(
                m => m.UserId == userId && m.Year == year && m.Month == month,
                cancellationToken);
    }

    public async Task AddMonthlyDistanceReadModelAsync(
        Domain.Entities.MonthlyDistanceReadModel readModel,
        CancellationToken cancellationToken = default)
    {
        await _context.MonthlyDistanceReadModels.AddAsync(readModel, cancellationToken);
    }

    public async Task<List<Domain.Entities.MonthlyDistanceReadModel>> GetMonthlyDistanceReadModelsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.MonthlyDistanceReadModels
            .ToListAsync(cancellationToken);
    }
}


