using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EveUp.Infrastructure.Jobs;

/// <summary>
/// Background job that cleans up expired and revoked refresh tokens.
/// Runs periodically via Hangfire to prevent token table bloat.
///
/// - Deletes tokens expired for more than 30 days
/// - Deletes revoked tokens older than 7 days
/// </summary>
public class TokenCleanupJob
{
    private readonly EveUpDbContext _context;
    private readonly ILogger<TokenCleanupJob> _logger;

    private static readonly TimeSpan ExpiredRetention = TimeSpan.FromDays(30);
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(7);

    public TokenCleanupJob(EveUpDbContext context, ILogger<TokenCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("[TokenCleanup] Starting token cleanup...");

        var now = DateTime.UtcNow;

        // Delete tokens that expired more than 30 days ago
        var expiredCutoff = now - ExpiredRetention;
        var expiredTokens = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < expiredCutoff)
            .ToListAsync();

        if (expiredTokens.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            _logger.LogInformation("[TokenCleanup] Removed {Count} expired tokens (expired before {Cutoff}).",
                expiredTokens.Count, expiredCutoff);
        }

        // Delete tokens that were revoked more than 7 days ago
        var revokedCutoff = now - RevokedRetention;
        var revokedTokens = await _context.RefreshTokens
            .Where(t => t.RevokedAt != null && t.RevokedAt < revokedCutoff)
            .ToListAsync();

        if (revokedTokens.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(revokedTokens);
            _logger.LogInformation("[TokenCleanup] Removed {Count} revoked tokens (revoked before {Cutoff}).",
                revokedTokens.Count, revokedCutoff);
        }

        await _context.SaveChangesAsync();

        var totalRemoved = expiredTokens.Count + revokedTokens.Count;
        _logger.LogInformation("[TokenCleanup] Cleanup completed. Total removed: {Total}", totalRemoved);
    }
}
