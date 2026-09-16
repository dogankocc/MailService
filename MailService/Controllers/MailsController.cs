using MailService.Data;
using MailService.Models;
using MailService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MailService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MailsController : ControllerBase
{
    private readonly IMailQueueService _queueService;
    private readonly IRateLimiterService _rateLimiter;
    private readonly ITemplateService _templateService;
    private readonly MailServiceDbContext _dbContext;
    private readonly ILogger<MailsController> _logger;

    public MailsController(
        IMailQueueService queueService,
        IRateLimiterService rateLimiter,
        ITemplateService templateService,
        MailServiceDbContext dbContext,
        ILogger<MailsController> logger)
    {
        _queueService = queueService;
        _rateLimiter = rateLimiter;
        _templateService = templateService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Send mail using template (requires mail.send scope)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MailSend")]
    public async Task<IActionResult> Send([FromBody] SendMailRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var clientId = User.FindFirst("client_id")?.Value ?? "unknown";
        var clientIdInt = GetClientIdAsInt(clientId);
        var mailCount = request.To.Count;

        // Rate Limit Check
        var rateCheck = await _rateLimiter.CheckAndRecordAsync(clientId, mailCount);
        if (!rateCheck.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for {ClientId}", clientId);
            return StatusCode(429, new
            {
                Error = "RateLimitExceeded",
                Message = "Daily or minute limit exceeded",
                DailyRemaining = rateCheck.DailyRemaining,
                MinuteRemaining = rateCheck.MinuteRemaining
            });
        }

        // Render template ONCE here - body and subject go to queue
        var rendered = await _templateService.RenderAsync(request.TemplateCode, request.Parameters);
        if (rendered == null)
        {
            _logger.LogWarning("Template not found: {Code}", request.TemplateCode);
            return BadRequest(new { Error = "TemplateNotFound", Code = request.TemplateCode });
        }

        var requestId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var clientIdName = clientId;

        // Audit Log: Record as Queued immediately
        foreach (var to in request.To)
        {
            _dbContext.MailLogs.Add(new MailLog
            {
                ClientId = clientIdInt,
                ClientIdName = clientIdName,
                To = to,
                Subject = rendered.Value.Subject,
                SentDate = now,
                Status = 4, // Queued
                RequestId = requestId
            });
        }
        await _dbContext.SaveChangesAsync();

        // Queue the message with RENDERED body and subject
        var message = new MailQueueMessage
        {
            RequestId = requestId,
            ClientId = clientIdInt,
            ClientIdName = clientIdName,
            TemplateCode = request.TemplateCode,
            To = request.To,
            Subject = rendered.Value.Subject,
            Body = rendered.Value.Body,
            CreatedAt = now
        };

        await _queueService.EnqueueAsync(message);

        _logger.LogInformation("Mail queued by {ClientId}: {RequestId} ({Count} recipients)",
            clientId, requestId, mailCount);

        return Ok(new
        {
            RequestId = requestId,
            Status = "Queued",
            RecipientCount = mailCount,
            DailyRemaining = rateCheck.DailyRemaining - mailCount,
            MinuteRemaining = rateCheck.MinuteRemaining - mailCount,
            QueuedAt = now
        });
    }

    /// <summary>
    /// Bulk send mails (requires mail.bulk.send scope)
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = "MailBulkSend")]
    public async Task<IActionResult> BulkSend([FromBody] List<SendMailRequest> requests)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var clientId = User.FindFirst("client_id")?.Value ?? "unknown";
        var clientIdInt = GetClientIdAsInt(clientId);

        // Calculate total mail count
        var totalCount = requests.Sum(r => r.To.Count);

        // Rate Limit Check
        var rateCheck = await _rateLimiter.CheckAndRecordAsync(clientId, totalCount);
        if (!rateCheck.IsAllowed)
        {
            return StatusCode(429, new
            {
                Error = "RateLimitExceeded",
                TotalRequested = totalCount,
                DailyRemaining = rateCheck.DailyRemaining,
                MinuteRemaining = rateCheck.MinuteRemaining
            });
        }

        var requestIds = new List<Guid>();
        var now = DateTime.UtcNow;
        var clientIdName = clientId;

        foreach (var request in requests)
        {
            // Render each template
            var rendered = await _templateService.RenderAsync(request.TemplateCode, request.Parameters);
            if (rendered == null)
            {
                _logger.LogWarning("Template not found in bulk: {Code}", request.TemplateCode);
                continue;
            }

            var requestId = Guid.NewGuid();
            requestIds.Add(requestId);

            // Audit Log
            foreach (var to in request.To)
            {
                _dbContext.MailLogs.Add(new MailLog
                {
                    ClientId = clientIdInt,
                    ClientIdName = clientIdName,
                    To = to,
                    Subject = rendered.Value.Subject,
                    SentDate = now,
                    Status = 4,
                    RequestId = requestId
                });
            }

            // Queue with rendered content
            var message = new MailQueueMessage
            {
                RequestId = requestId,
                ClientId = clientIdInt,
                ClientIdName = clientIdName,
                TemplateCode = request.TemplateCode,
                To = request.To,
                Subject = rendered.Value.Subject,
                Body = rendered.Value.Body,
                CreatedAt = now
            };

            await _queueService.EnqueueAsync(message);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            RequestIds = requestIds,
            TotalQueued = requestIds.Count,
            TotalRecipients = totalCount,
            DailyRemaining = rateCheck.DailyRemaining - totalCount,
            MinuteRemaining = rateCheck.MinuteRemaining - totalCount,
            Status = "Queued",
            QueuedAt = now
        });
    }

    /// <summary>
    /// Get mail status by requestId
    /// </summary>
    [HttpGet("status/{requestId}")]
    [Authorize]
    public async Task<IActionResult> GetStatus(Guid requestId)
    {
        var clientId = User.FindFirst("client_id")?.Value;
        var isAdmin = User.HasClaim(c =>
        {
            var scopes = c.Value.Split(' ');
            return scopes.Contains("mail.admin");
        });

        var logs = await _dbContext.MailLogs
            .Where(l => l.RequestId == requestId)
            .ToListAsync();

        if (logs.Count == 0)
            return NotFound();

        if (!isAdmin && logs.All(l => l.ClientIdName != clientId))
            return Forbid();

        var statusMap = new Dictionary<int, string>
        {
            { 0, "Failed" },
            { 1, "Success" },
            { 2, "TemplateNotFound" },
            { 3, "RateLimited" },
            { 4, "Queued" }
        };

        return Ok(new
        {
            RequestId = requestId,
            Total = logs.Count,
            SuccessCount = logs.Count(l => l.Status == 1),
            FailedCount = logs.Count(l => l.Status == 0),
            QueuedCount = logs.Count(l => l.Status == 4),
            Recipients = logs.Select(l => new
            {
                l.To,
                l.Subject,
                Status = statusMap.GetValueOrDefault(l.Status, "Unknown"),
                l.SentDate
            })
        });
    }

    /// <summary>
    /// Search logs with filters (requires mail.admin scope)
    /// Both startDate and endDate are required when using date filter
    /// </summary>
    [HttpGet("search")]
    [Authorize(Policy = "MailAdmin")]
    public async Task<IActionResult> SearchLogs(
        [FromQuery] Guid? requestId = null,
        [FromQuery] string? clientIdName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _dbContext.MailLogs.AsQueryable();

        // RequestId filtresi (tek başına kullanılabilir)
        if (requestId.HasValue)
        {
            query = query.Where(l => l.RequestId == requestId.Value);
        }

        // Tarih aralığı ve clientIdName: İKİSİ DE GİRİLMELİ
        var hasDateFilter = startDate.HasValue && endDate.HasValue;
        var hasClientFilter = !string.IsNullOrEmpty(clientIdName);

        if (hasDateFilter != hasClientFilter)
        {
            return BadRequest(new { Error = "Both startDate/endDate and clientIdName must be provided together" });
        }

        if (hasDateFilter && hasClientFilter)
        {
            query = query.Where(l =>
                l.SentDate >= startDate.Value &&
                l.SentDate <= endDate.Value &&
                l.ClientIdName == clientIdName);
        }

        // Eğer hiç filtre yoksa ve requestId yoksa hata
        if (!requestId.HasValue && !hasDateFilter)
        {
            return BadRequest(new { Error = "Provide either requestId, or (startDate, endDate, clientIdName) together" });
        }

        var logs = await query
            .OrderByDescending(l => l.SentDate)
            .Take(500) // Limitation
            .ToListAsync();

        var statusMap = new Dictionary<int, string>
        {
            { 0, "Failed" },
            { 1, "Success" },
            { 2, "TemplateNotFound" },
            { 3, "RateLimited" },
            { 4, "Queued" }
        };

        var successCount = logs.Count(l => l.Status == 1);
        var failedCount = logs.Count(l => l.Status == 0);
        var queuedCount = logs.Count(l => l.Status == 4);

        return Ok(new
        {
            Total = logs.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            QueuedCount = queuedCount,
            Logs = logs.Select(l => new
            {
                l.Id,
                l.RequestId,
                l.ClientIdName,
                l.To,
                l.Subject,
                Status = statusMap.GetValueOrDefault(l.Status, "Unknown"),
                l.SentDate
            })
        });
    }

    private static int GetClientIdAsInt(string clientId)
    {
        return Math.Abs(clientId.GetHashCode());
    }
}
