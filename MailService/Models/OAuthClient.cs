namespace MailService.Models;

public class OAuthClient
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty; // "mail.send mail.bulk.send"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
}
