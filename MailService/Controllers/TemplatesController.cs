using MailService.Data;
using MailService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MailService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly MailServiceDbContext _dbContext;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(MailServiceDbContext dbContext, ILogger<TemplatesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all active templates (requires mail.template.read or mail.admin)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "MailTemplateRead")]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _dbContext.MailTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync();

        return Ok(templates);
    }

    /// <summary>
    /// Get single template by code (requires mail.template.read or mail.admin)
    /// </summary>
    [HttpGet("{code}")]
    [Authorize(Policy = "MailTemplateRead")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var template = await _dbContext.MailTemplates
            .FirstOrDefaultAsync(t => t.Code == code && t.IsActive);

        if (template == null)
            return NotFound();

        return Ok(template);
    }

    /// <summary>
    /// Create new template (requires mail.template.write or mail.admin)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "MailTemplateWrite")]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var exists = await _dbContext.MailTemplates.AnyAsync(t => t.Code == request.Code);
        if (exists)
            return Conflict($"Template with code '{request.Code}' already exists");

        var template = new MailTemplate
        {
            Code = request.Code,
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Body,
            Version = 1,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.MailTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Template created: {Code} v{Version}", template.Code, template.Version);

        return CreatedAtAction(nameof(GetByCode), new { code = template.Code }, template);
    }

    /// <summary>
    /// Update template (creates new version) (requires mail.template.write or mail.admin)
    /// </summary>
    [HttpPut("{code}")]
    [Authorize(Policy = "MailTemplateWrite")]
    public async Task<IActionResult> Update(string code, [FromBody] CreateTemplateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existing = await _dbContext.MailTemplates
            .FirstOrDefaultAsync(t => t.Code == code);

        if (existing == null)
            return NotFound();

        // Deactivate old version
        existing.IsActive = false;
        existing.UpdatedDate = DateTime.UtcNow;

        // Create new version
        var newVersion = new MailTemplate
        {
            Code = request.Code,
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Body,
            Version = existing.Version + 1,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.MailTemplates.Add(newVersion);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Template updated: {Code} v{Version} -> v{NewVersion}",
            existing.Code, existing.Version, newVersion.Version);

        return Ok(newVersion);
    }

    /// <summary>
    /// Delete (deactivate) template (requires mail.admin)
    /// </summary>
    [HttpDelete("{code}")]
    [Authorize(Policy = "MailAdmin")]
    public async Task<IActionResult> Delete(string code)
    {
        var templates = await _dbContext.MailTemplates
            .Where(t => t.Code == code)
            .ToListAsync();

        if (templates.Count == 0)
            return NotFound();

        foreach (var t in templates)
        {
            t.IsActive = false;
            t.UpdatedDate = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Template deactivated: {Code}", code);

        return NoContent();
    }
}
