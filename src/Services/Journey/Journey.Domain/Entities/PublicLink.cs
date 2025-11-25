using Shared.Common.Result;

namespace Journey.Domain.Entities;

/// <remarks>
/// Excluded from code coverage: Simple entity class with token generation.
/// Tested via integration tests.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Simple entity class. Tested via integration tests.")]
public sealed class PublicLink
{
    private PublicLink()
    {
    }

    private PublicLink(Guid journeyId, string token, string createdByUserId)
    {
        Id = Guid.NewGuid();
        JourneyId = journeyId;
        Token = token;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
        IsRevoked = false;
    }

    public Guid Id { get; private set; }
    public Guid JourneyId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedOnUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }

    public Journey Journey { get; private set; } = null!;

    public static Result<PublicLink> Create(Guid journeyId, string createdByUserId)
    {
        var token = GenerateSecureToken();
        var link = new PublicLink(journeyId, token, createdByUserId);
        return Result.Success(link);
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedOnUtc = DateTime.UtcNow;
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

