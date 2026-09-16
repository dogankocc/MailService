using MailService.Data;
using Microsoft.EntityFrameworkCore;

namespace MailService.Services;

public class RateLimiterService : IRateLimiterService
{
    private readonly MailServiceDbContext _dbContext;
    private readonly ILogger<RateLimiterService> _logger;

    // Default limits if client not registered
    private const int DefaultDailyLimit = 100;
    private const int DefaultMinuteLimit = 10;

    public RateLimiterService(MailServiceDbContext dbContext, ILogger<RateLimiterService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(bool IsAllowed, int DailyRemaining, int MinuteRemaining)> CheckAndRecordAsync(string clientId, int mailCount = 1)
    {
        // Get or create client config
        var client = await _dbContext.ClientApplications
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        int dailyLimit = client?.DailyLimit ?? DefaultDailyLimit;
        int minuteLimit = client?.MinuteLimit ?? DefaultMinuteLimit;

        // Check if client is active
        if (client != null && !client.IsActive)
        {
            _logger.LogWarning("Inactive client tried to send: {ClientId}", clientId);
            return (false, 0, 0);
        }

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var minuteStart = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);

        // Count today's mails
        var dailyCount = await _dbContext.MailLogs
            .CountAsync(l => l.ClientIdName == clientId && l.SentDate >= todayStart);

        // Count this minute's mails
        var minuteCount = await _dbContext.MailLogs
            .CountAsync(l => l.ClientIdName == clientId && l.SentDate >= minuteStart);

        int dailyRemaining = dailyLimit - dailyCount;
        int minuteRemaining = minuteLimit - minuteCount;

        bool isAllowed = dailyRemaining >= mailCount && minuteRemaining >= mailCount;

        if (!isAllowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {ClientId}: Daily={DailyCount}/{DailyLimit}, Minute={MinuteCount}/{MinuteLimit}",
                clientId, dailyCount, dailyLimit, minuteCount, minuteLimit);
        }

        return (isAllowed, Math.Max(0, dailyRemaining), Math.Max(0, minuteRemaining));
    }
}
