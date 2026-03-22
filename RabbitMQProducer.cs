using RabbitMQ.Client;
using System;
using System.Text;

public class RabbitMQProducer
{
    public void SendMessage(string message)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "ecommerce_user",
            Password = "123456", // ⚠️ đổi đúng mật khẩu bạn tạo
            VirtualHost = "ecommerce_vhost"
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // tạo queue
        channel.QueueDeclare(
            queue: "TestQueue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(message);

        // gửi message
        channel.BasicPublish(
            exchange: "",
            routingKey: "TestQueue",
            basicProperties: null,
            body: body);

        Console.WriteLine("Sent: " + message);
    }
}