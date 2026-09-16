namespace MailService.Models;

public class ClientApplicationRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DailyLimit { get; set; } = 1000;
    public int MinuteLimit { get; set; } = 100;
}
