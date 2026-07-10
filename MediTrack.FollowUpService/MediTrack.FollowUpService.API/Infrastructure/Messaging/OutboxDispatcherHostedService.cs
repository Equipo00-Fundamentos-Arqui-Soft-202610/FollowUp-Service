using System.Text;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace MediTrack.FollowUpService.API.Infrastructure.Messaging;

/// <summary>
/// Lee periódicamente los mensajes pendientes del Outbox y los entrega a
/// RabbitMQ usando el EventType como routing key. Marca cada mensaje como
/// procesado solo tras confirmar la publicación (entrega "al menos una vez").
/// </summary>
public sealed class OutboxDispatcherHostedService : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxAttempts = 10;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<OutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error en el ciclo del publicador de Outbox.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FollowUpDbContext>();

        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        var rabbitMqSection = _configuration.GetSection("RabbitMq");
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqSection["Host"] ?? "localhost",
            Port = rabbitMqSection.GetValue("Port", 5672),
            UserName = rabbitMqSection["UserName"] ?? "guest",
            Password = rabbitMqSection["Password"] ?? "guest",
            VirtualHost = rabbitMqSection["VirtualHost"] ?? "/"
        };
        var exchangeName = rabbitMqSection["ExchangeName"] ?? "meditrack.events";

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ConfirmSelect();

        var unroutableMessageIds = new HashSet<string>();
        channel.BasicReturn += (_, args) =>
        {
            unroutableMessageIds.Add(args.BasicProperties.MessageId);
            _logger.LogError(
                "Mensaje de Outbox {MessageId} no pudo ser ruteado (sin binding activo): {ReplyText}",
                args.BasicProperties.MessageId, args.ReplyText);
        };

        foreach (var message in pending)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message.Payload);
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.Id.ToString();
                properties.Type = message.EventType;
                properties.ContentType = "application/json";

                channel.BasicPublish(exchangeName, message.EventType, mandatory: true, properties, body);
                channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                if (unroutableMessageIds.Contains(properties.MessageId))
                {
                    message.Attempts++;
                    message.LastError = "Sin cola/binding activo en el momento del publish; se reintentará.";
                    LogIfAbandoned(message);
                }
                else
                {
                    message.ProcessedAtUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                _logger.LogError(ex, "No se pudo publicar el mensaje de Outbox {MessageId}.", message.Id);
                LogIfAbandoned(message);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Publicados {Count} mensajes del Outbox.", pending.Count(m => m.ProcessedAtUtc != null));
    }

    /// <summary>
    /// Evita el reintento infinito de un mensaje "veneno" (p. ej. un EventType sin
    /// ningún binding activo): tras <see cref="MaxAttempts"/> intentos se deja de
    /// reintentar y se registra una alerta explícita para investigación manual.
    /// </summary>
    private void LogIfAbandoned(OutboxMessage message)
    {
        if (message.Attempts >= MaxAttempts)
        {
            _logger.LogError(
                "Mensaje de Outbox {MessageId} ({EventType}) alcanzó el máximo de {MaxAttempts} intentos; se deja de reintentar. Último error: {LastError}",
                message.Id, message.EventType, MaxAttempts, message.LastError);
        }
    }
}
