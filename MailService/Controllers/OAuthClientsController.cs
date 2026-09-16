using MailService.Data;
using MailService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MailService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "MailAdmin")]
public class OAuthClientsController : ControllerBase
{
    private readonly MailServiceDbContext _dbContext;
    private readonly ILogger<OAuthClientsController> _logger;

    public OAuthClientsController(MailServiceDbContext dbContext, ILogger<OAuthClientsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _dbContext.OAuthClients
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("{clientId}")]
    public async Task<IActionResult> GetByClientId(string clientId)
    {
        var client = await _dbContext.OAuthClients
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
            return NotFound();

        // Don't return secret in GET
        return Ok(new
        {
            client.Id,
            client.ClientId,
            client.Name,
            client.Scopes,
            client.IsActive,
            client.CreatedDate
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOAuthClientRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var exists = await _dbContext.OAuthClients.AnyAsync(c => c.ClientId == request.ClientId);
        if (exists)
            return Conflict($"Client '{request.ClientId}' already exists");

        var client = new OAuthClient
        {
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
            Name = request.Name,
            Scopes = request.Scopes,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.OAuthClients.Add(client);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("OAuth Client created: {ClientId} ({Name})", client.ClientId, client.Name);

        return CreatedAtAction(nameof(GetByClientId), new { clientId = client.ClientId }, new
        {
            client.Id,
            client.ClientId,
            client.Name,
            client.Scopes,
            client.IsActive,
            client.CreatedDate
        });
    }

    [HttpPut("{clientId}")]
    public async Task<IActionResult> Update(string clientId, [FromBody] CreateOAuthClientRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var client = await _dbContext.OAuthClients
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
            return NotFound();

        client.Name = request.Name;
        client.Scopes = request.Scopes;
        if (!string.IsNullOrEmpty(request.ClientSecret))
        {
            client.ClientSecret = request.ClientSecret;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("OAuth Client updated: {ClientId}", clientId);

        return Ok(new
        {
            client.Id,
            client.ClientId,
            client.Name,
            client.Scopes,
            client.IsActive
        });
    }

    [HttpDelete("{clientId}")]
    public async Task<IActionResult> Delete(string clientId)
    {
        var client = await _dbContext.OAuthClients
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
            return NotFound();

        client.IsActive = false;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("OAuth Client deactivated: {ClientId}", clientId);

        return NoContent();
    }
}

public class CreateOAuthClientRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string Scopes { get; set; } = string.Empty;
}
