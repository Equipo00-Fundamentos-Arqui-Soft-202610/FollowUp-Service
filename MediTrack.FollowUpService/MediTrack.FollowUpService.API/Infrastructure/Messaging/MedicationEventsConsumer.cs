using System.Text;
using System.Text.Json;
using MediTrack.FollowUpService.API.Application.Internal.EventHandlers;
using MediTrack.FollowUpService.API.Application.OutboundEvents;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MediTrack.FollowUpService.API.Infrastructure.Messaging;

public class MedicationEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MedicationEventsConsumer> _logger;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IModel? _channel;

    public MedicationEventsConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<MedicationEventsConsumer> logger,
        IOptions<RabbitMqOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost ?? "/"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);

        var queueName = "followup-service.medication-events";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, _options.ExchangeName, routingKey: "MedicationCancelled");
        _channel.QueueBind(queueName, _options.ExchangeName, routingKey: "MedicationUpdated");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                using var scope = _scopeFactory.CreateScope();

                if (ea.RoutingKey == "MedicationCancelled")
                {
                    var evt = JsonSerializer.Deserialize<MedicationCancelledEvent>(json);
                    if (evt is not null)
                    {
                        var handler = scope.ServiceProvider
                            .GetRequiredService<IMedicationCancelledEventHandler>();
                        await handler.HandleAsync(evt);
                    }
                }
                else if (ea.RoutingKey == "MedicationUpdated")
                {
                    var evt = JsonSerializer.Deserialize<MedicationUpdatedEvent>(json);
                    if (evt is not null)
                    {
                        var handler = scope.ServiceProvider
                            .GetRequiredService<IMedicationUpdatedEventHandler>();
                        await handler.HandleAsync(evt);
                    }
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process medication event with RoutingKey {RoutingKey}", ea.RoutingKey);
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
