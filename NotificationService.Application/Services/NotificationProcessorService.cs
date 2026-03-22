using NotificationService.Contracts.Interfaces;
namespace NotificationService.Application.Services
{
    public class NotificationProcessorService : INotificationProcessor
    {
        public async Task ProcessQueueBatchAsync(int take, int skip)
        {
            Console.WriteLine($"[NotificationProcessor] Batch take={take} skip={skip}");
            await Task.CompletedTask;
        }
    }
}
