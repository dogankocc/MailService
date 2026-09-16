namespace MailService.Services;

public interface ITemplateService
{
    Task<(string Subject, string Body)?> RenderAsync(string templateCode, Dictionary<string, string> parameters);
}
