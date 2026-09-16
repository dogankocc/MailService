using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MailService.Services;

public class SendGridMailSender : IMailSender
{
    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridMailSender> _logger;

    public SendGridMailSender(IOptions<SendGridSettings> settings, ILogger<SendGridMailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string to, string subject, string htmlBody, string plainTextBody)
    {
        try
        {
            var client = new SendGridClient(_settings.ApiKey);
            var from = new EmailAddress(_settings.SenderEmail, _settings.SenderName);
            var toAddress = new EmailAddress(to);

            var msg = MailHelper.CreateSingleEmail(
                from,
                toAddress,
                subject,
                plainTextBody,
                htmlBody);

            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Mail sent via SendGrid to {To}", to);
                return true;
            }

            var responseBody = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid failed for {To}: Status={StatusCode}, Response={Response}",
                to, response.StatusCode, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send mail via SendGrid to {To}", to);
            return false;
        }
    }
}

public class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
}
