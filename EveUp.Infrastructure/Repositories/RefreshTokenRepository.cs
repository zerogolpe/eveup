using EveUp.Core.Entities;
using EveUp.Core.Interfaces;
using EveUp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EveUp.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly EveUpDbContext _context;

    public RefreshTokenRepository(EveUpDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == tokenHash);
    }

    public async Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<List<RefreshToken>> GetByFamilyAsync(string family)
    {
        return await _context.RefreshTokens
            .Where(t => t.TokenFamily == family)
            .ToListAsync();
    }

    public async Task DeleteExpiredAsync(DateTime before)
    {
        var expired = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < before)
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(expired);
        await _context.SaveChangesAsync();
    }
}
