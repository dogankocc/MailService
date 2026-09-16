using MailService.Models;

namespace MailService.Services;

public interface IMailQueueService
{
    Task EnqueueAsync(MailQueueMessage message);
}
