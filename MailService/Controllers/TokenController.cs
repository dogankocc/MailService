using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MailService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MailService.Controllers;

[ApiController]
[Route("[controller]")]
public class TokenController : ControllerBase
{
    private readonly MailServiceDbContext _dbContext;
    private readonly IConfiguration _config;
    private readonly ILogger<TokenController> _logger;

    public TokenController(MailServiceDbContext dbContext, IConfiguration config, ILogger<TokenController> logger)
    {
        _dbContext = dbContext;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// OAuth2 Client Credentials Token Endpoint
    /// POST /token
    /// </summary>
    [HttpPost("connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] TokenRequest request)
    {
        _logger.LogInformation("Token request: client_id={ClientId}, grant_type={GrantType}", request.ClientId, request.GrantType);

        if (request.GrantType != "client_credentials")
        {
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        // Find client
        var client = await _dbContext.OAuthClients
            .FirstOrDefaultAsync(c => c.ClientId == request.ClientId && c.IsActive);

        if (client == null || client.ClientSecret != request.ClientSecret)
        {
            _logger.LogWarning("Invalid client credentials: {ClientId}", request.ClientId);
            return Unauthorized(new { error = "invalid_client" });
        }

        // Parse scopes - requested scopes, default to all client scopes
        var clientScopes = client.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var requestedScopes = string.IsNullOrEmpty(request.Scope)
            ? clientScopes
            : request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Intersection - only grant scopes that client has
        var grantedScopes = requestedScopes.Intersect(clientScopes).ToList();

        if (grantedScopes.Count == 0 && requestedScopes.Count() > 0)
        {
            _logger.LogWarning("Client {ClientId} requested invalid scopes: {Scopes}", request.ClientId, request.Scope);
            return BadRequest(new { error = "invalid_scope" });
        }

        // Generate JWT
        var token = GenerateJwtToken(client.ClientId, grantedScopes);

        _logger.LogInformation("Token issued for {ClientId}, scopes: {Scopes}", client.ClientId, string.Join(" ", grantedScopes));

        return Ok(new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = 3600, // 1 hour
            scope = string.Join(" ", grantedScopes)
        });
    }

    private string GenerateJwtToken(string clientId, List<string> scopes)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "MailServiceSecretKeyMinimum32CharactersLong12345"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", clientId),
            new Claim("client_id", clientId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        // Add scopes - both as single space-separated and individual claims
        claims.Add(new Claim("scope", string.Join(" ", scopes)));
        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "https://localhost",
            audience: _config["Jwt:Audience"] ?? "mail-service",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class TokenRequest
{
    public string GrantType { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? Scope { get; set; }
}
