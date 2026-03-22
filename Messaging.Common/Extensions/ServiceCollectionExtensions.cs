using Microsoft.Extensions.DependencyInjection;
using Messaging.Common.Connection;
namespace Messaging.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMq(
            this IServiceCollection services,
            string hostName, string userName, string password, string vhost)
        {
            var cm = new ConnectionManager(hostName, userName, password, vhost);
            var connection = cm.GetConnection();
            var channel = connection.CreateModel();
            services.AddSingleton(cm);
            services.AddSingleton(connection);
            services.AddSingleton(channel);
            return services;
        }
    }
}
