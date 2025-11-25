using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Gateway.Infrastructure.Authentication;

/// <remarks>
/// Excluded from code coverage: Infrastructure service for distributed cache ticket storage.
/// Ticket storage operations are tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Infrastructure service - ticket storage tested via integration tests.")]
public class DistributedCacheTicketStore : ITicketStore
{
    private readonly IDistributedCache _cache;
    private const string KeyPrefix = "AuthTicket:";

    public DistributedCacheTicketStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = $"{KeyPrefix}{Guid.NewGuid()}";
        var serializedTicket = TicketSerializer.Default.Serialize(ticket);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ticket.Properties.ExpiresUtc?.Subtract(DateTimeOffset.UtcNow) ?? TimeSpan.FromHours(8),
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };

        await _cache.SetAsync(key, serializedTicket, options);
        return key;
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var serializedTicket = await _cache.GetAsync(key);
        if (serializedTicket == null || serializedTicket.Length == 0)
        {
            return null;
        }

        return TicketSerializer.Default.Deserialize(serializedTicket);
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var serializedTicket = TicketSerializer.Default.Serialize(ticket);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ticket.Properties.ExpiresUtc?.Subtract(DateTimeOffset.UtcNow) ?? TimeSpan.FromHours(8),
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };

        await _cache.SetAsync(key, serializedTicket, options);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}

