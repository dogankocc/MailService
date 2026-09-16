namespace MailService.Services;

public interface IRateLimiterService
{
    /// <summary>
    /// Checks if client is within rate limits and records the request
    /// </summary>
    /// <returns>(IsAllowed, DailyRemaining, MinuteRemaining)</returns>
    Task<(bool IsAllowed, int DailyRemaining, int MinuteRemaining)> CheckAndRecordAsync(string clientId, int mailCount = 1);
}
