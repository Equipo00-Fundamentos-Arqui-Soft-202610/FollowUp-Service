using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace MediTrack.FollowUpService.API.Infrastructure.Messaging;

public class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly object _connectionLock = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task PublishAsync(string routingKey, object payload)
    {
        using var channel = GetConnection().CreateModel();
        var exchangeName = GetExchangeName();

        channel.ExchangeDeclare(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = routingKey;

        channel.BasicPublish(
            exchange: exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _connection?.Dispose();
        _disposed = true;
    }

    private IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        lock (_connectionLock)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _connection?.Dispose();
            _connection = CreateConnection();
            return _connection;
        }
    }

    private IConnection CreateConnection()
    {
        var rabbitMqSection = _configuration.GetSection("RabbitMq");
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqSection["Host"] ?? "localhost",
            Port = rabbitMqSection.GetValue("Port", 5672),
            UserName = rabbitMqSection["UserName"] ?? "guest",
            Password = rabbitMqSection["Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        return factory.CreateConnection();
    }

    private string GetExchangeName()
    {
        return _configuration["RabbitMq:ExchangeName"] ?? "meditrack.events";
    }
}
