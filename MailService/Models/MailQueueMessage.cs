namespace MailService.Models;

public class MailQueueMessage
{
    public Guid RequestId { get; set; }
    public int ClientId { get; set; }
    public string ClientIdName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public List<string> To { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
