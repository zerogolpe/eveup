using EveUp.Core.Enums;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EveUp.Infrastructure.Jobs;

/// <summary>
/// Job que roda periodicamente para desbanir usuários cujo ban temporário expirou
/// </summary>
public class UnbanExpiredUsersJob
{
    private readonly EveUpDbContext _context;
    private readonly ILogger<UnbanExpiredUsersJob> _logger;

    public UnbanExpiredUsersJob(EveUpDbContext context, ILogger<UnbanExpiredUsersJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            var expiredBans = await _context.Users
                .Where(u => u.State == UserState.BANNED &&
                           u.BanExpires != null &&
                           u.BanExpires <= now)
                .ToListAsync();

            if (expiredBans.Any())
            {
                _logger.LogInformation("Unbanning {Count} users with expired bans", expiredBans.Count);

                foreach (var user in expiredBans)
                {
                    user.Unban();
                    _logger.LogInformation("Unbanned user {UserId}", user.Id);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully unbanned {Count} users", expiredBans.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unbanning expired users");
            throw;
        }
    }
}
