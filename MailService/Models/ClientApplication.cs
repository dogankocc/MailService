namespace MailService.Models;

public class ClientApplication
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DailyLimit { get; set; } = 1000;
    public int MinuteLimit { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
