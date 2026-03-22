using RabbitMQ.Client;
namespace Messaging.Common.Connection
{
    public class ConnectionManager
    {
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        public ConnectionManager(string hostName, string userName, string password, string vhost)
        {
            _factory = new ConnectionFactory
            {
                HostName = hostName, UserName = userName,
                Password = password, VirtualHost = vhost,
                DispatchConsumersAsync = true
            };
        }
        public IConnection GetConnection()
        {
            if (_connection == null || !_connection.IsOpen)
                _connection = _factory.CreateConnection();
            return _connection;
        }
    }
}
