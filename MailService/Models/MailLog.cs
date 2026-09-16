namespace MailService.Models;

public class MailLog
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientIdName { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime SentDate { get; set; }
    public int Status { get; set; } // 0: Failed, 1: Success, 2: TemplateNotFound, 3: RateLimited, 4: Queued
    public Guid RequestId { get; set; }
}
