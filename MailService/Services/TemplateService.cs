using System.Text.RegularExpressions;
using MailService.Data;
using Microsoft.EntityFrameworkCore;

namespace MailService.Services;

public class TemplateService : ITemplateService
{
    private readonly MailServiceDbContext _dbContext;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(MailServiceDbContext dbContext, ILogger<TemplateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(string Subject, string Body)?> RenderAsync(string templateCode, Dictionary<string, string> parameters)
    {
        var template = await _dbContext.MailTemplates
            .FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive);

        if (template == null)
        {
            _logger.LogWarning("Template not found: {Code}", templateCode);
            return null;
        }

        return (
            ReplacePlaceholders(template.Subject, parameters),
            ReplacePlaceholders(template.Body, parameters)
        );
    }

    private static string ReplacePlaceholders(string input, Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // {{ParameterName}} formatını destekle
        return Regex.Replace(input, @"\{\{(\w+)\}\}", match =>
        {
            var key = match.Groups[1].Value;
            return parameters.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
