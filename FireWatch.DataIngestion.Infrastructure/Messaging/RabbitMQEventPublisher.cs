using FireWatch.DataIngestion.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FireWatch.DataIngestion.Infrastructure.Messaging;

public class RabbitMQEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly ConnectionFactory _factory;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string Exchange = "firewatch.events";

    public RabbitMQEventPublisher(
        IConfiguration config,
        ILogger<RabbitMQEventPublisher> logger)
    {
        _logger = logger;

        _factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:User"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };
    }

    private async Task EnsureInitializedAsync()
    {
        if (_connection is not null && _channel is not null)
            return;

        _connection = await _factory.CreateConnectionAsync(
            "firewatch-data-ingestion");

        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
    }

    public async Task PublishAsync<T>(
        T @event,
        string routingKey,
        CancellationToken ct = default)
        where T : class
    {
        await EnsureInitializedAsync();

        var body = JsonSerializer.SerializeToUtf8Bytes(@event);

        await _channel!.BasicPublishAsync(
            exchange: Exchange,
            routingKey: routingKey,
            mandatory: false,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation(
            "Publicado: {EventType} → {Exchange}/{Key}",
            typeof(T).Name,
            Exchange,
            routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}