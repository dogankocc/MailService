namespace MailService.Services;

public interface IMailSender
{
    Task<bool> SendAsync(string to, string subject, string htmlBody, string plainTextBody);
}
