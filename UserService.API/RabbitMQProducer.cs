using RabbitMQ.Client;
using System.Text;

public class RabbitMQProducer
{
    public async Task SendMessage(string message)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "ecommerce_user",
            Password = "123456",
            VirtualHost = "ecommerce_vhost"
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "TestQueue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "TestQueue",
            body: body);

        Console.WriteLine("Sent: " + message);
    }
}