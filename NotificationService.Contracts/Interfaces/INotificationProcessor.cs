namespace NotificationService.Contracts.Interfaces
{
    public interface INotificationProcessor
    {
        Task ProcessQueueBatchAsync(int take, int skip);
    }
}
