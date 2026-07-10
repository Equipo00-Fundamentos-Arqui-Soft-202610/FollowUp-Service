using System.Text;
using System.Text.Json;
using MediTrack.FollowUpService.API.Application.Internal.EventHandlers;
using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MediTrack.FollowUpService.API.Infrastructure.Messaging;

public class PrescriptionCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrescriptionCreatedConsumer> _logger;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IModel? _channel;

    public PrescriptionCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PrescriptionCreatedConsumer> logger,
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

        var dlxName = "followup-service.dlx";
        var dlqName = "followup-service.dlq";
        _channel.ExchangeDeclare(dlxName, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(dlqName, dlxName, routingKey: "");

        var queueName = "followup-service.prescription-created";
        var queueArgs = new Dictionary<string, object> { { "x-dead-letter-exchange", dlxName } };
        try
        {
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            _logger.LogWarning("La cola {QueueName} existía con argumentos distintos; se recrea con soporte de DLQ.", queueName);
            _channel = _connection!.CreateModel();
            _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);
            _channel.ExchangeDeclare(dlxName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(dlqName, dlxName, routingKey: "");
            _channel.QueueDelete(queueName);
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        }
        _channel.QueueBind(queueName, _options.ExchangeName, routingKey: "PrescriptionCreated");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<PrescriptionCreatedEvent>(json);
                if (evt is not null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();

                    var alreadyProcessed = await context.ProcessedEvents.AnyAsync(e => e.EventId == evt.EventId);
                    if (alreadyProcessed)
                    {
                        _logger.LogInformation("Evento {EventId} ya procesado; se omite.", evt.EventId);
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    var handler = scope.ServiceProvider
                        .GetRequiredService<IPrescriptionCreatedEventHandler>();
                    await handler.HandleAsync(evt);

                    context.ProcessedEvents.Add(new ProcessedEvent
                    {
                        EventId = evt.EventId,
                        EventType = "PrescriptionCreated",
                        ProcessedAtUtc = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                if (ea.Redelivered)
                {
                    _logger.LogError(ex, "Error persistente procesando evento; se envía a DLQ.");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
                else
                {
                    _logger.LogWarning(ex, "Error procesando evento; se reintenta una vez.");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
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
