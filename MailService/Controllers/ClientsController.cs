using MailService.Data;
using MailService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MailService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "MailAdmin")]
public class ClientsController : ControllerBase
{
    private readonly MailServiceDbContext _dbContext;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(MailServiceDbContext dbContext, ILogger<ClientsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _dbContext.ClientApplications
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("{clientId}")]
    public async Task<IActionResult> GetByClientId(string clientId)
    {
        var client = await _dbContext.ClientApplications
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
            return NotFound();

        return Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientApplicationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var exists = await _dbContext.ClientApplications.AnyAsync(c => c.ClientId == request.ClientId);
        if (exists)
            return Conflict($"Client '{request.ClientId}' already exists");

        var client = new ClientApplication
        {
            ClientId = request.ClientId,
            Name = request.Name,
            DailyLimit = request.DailyLimit,
            MinuteLimit = request.MinuteLimit,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.ClientApplications.Add(client);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Client created: {ClientId} ({Name})", client.ClientId, client.Name);

        return CreatedAtAction(nameof(GetByClientId), new { clientId = client.ClientId }, client);
    }

    [HttpPut("{clientId}")]
    public async Task<IActionResult> Update(string clientId, [FromBody] ClientApplicationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var client = await _dbContext.ClientApplications
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
            return NotFound();

        client.Name = request.Name;
        client.DailyLimit = request.DailyLimit;
        client.MinuteLimit = request.MinuteLimit;
        client.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Client updated: {ClientId}", clientId);

        return Ok(client);
    }

    [HttpDelete("{clientId}")]
    public async Task<IActionResult> Delete(string clientId)
    {
        var client = await _dbContext.ClientApplications
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
            return NotFound();

        client.IsActive = false;
        client.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Client deactivated: {ClientId}", clientId);

        return NoContent();
    }
}
