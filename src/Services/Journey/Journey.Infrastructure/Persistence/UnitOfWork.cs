using Journey.Application.Interfaces;

namespace Journey.Infrastructure.Persistence;

/// <remarks>
/// Excluded from code coverage: Infrastructure Unit of Work pattern implementation.
/// Database transactions are tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure UnitOfWork. Tested via integration tests.")]
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly JourneyDbContext _context;

    public UnitOfWork(JourneyDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}

