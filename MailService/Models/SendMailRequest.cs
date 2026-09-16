namespace MailService.Models;

public class SendMailRequest
{
    public string TemplateCode { get; set; } = string.Empty;
    public List<string> To { get; set; } = new();
    public Dictionary<string, string> Parameters { get; set; } = new();
}
