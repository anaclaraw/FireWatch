using FireWatch.RiskAnalysis.Services.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FireWatch.RiskAnalysis.Messaging;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMQConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string Exchange = "firewatch.events";
    private const string Queue = "firewatch.risk.analysis";
    private const string RoutingKey = "firewatch.spatial.received";

    public RabbitMQConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<RabbitMQConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await ConnectWithRetry(ct);

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(1000, ct);
        }
    }

    private async Task ConnectWithRetry(CancellationToken ct)
    {
        var attempts = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                attempts++;

                _logger.LogInformation(
                    "Conectando ao RabbitMQ (tentativa {N})...",
                    attempts);

                var factory = new ConnectionFactory
                {
                    HostName = _config["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
                    UserName = _config["RabbitMQ:User"] ?? "guest",
                    Password = _config["RabbitMQ:Password"] ?? "guest"
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange: Exchange,
                    type: ExchangeType.Topic,
                    durable: true);

                await _channel.QueueDeclareAsync(
                    queue: Queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                await _channel.QueueBindAsync(
                    queue: Queue,
                    exchange: Exchange,
                    routingKey: RoutingKey);

                await _channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += OnMessageReceived;

                await _channel.BasicConsumeAsync(
                    queue: Queue,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation(
                    "Consumer conectado. Escutando fila '{Queue}'...",
                    Queue);

                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Falha ao conectar ao RabbitMQ: {Msg}. Tentando em 5s...",
                    ex.Message);

                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task OnMessageReceived(
        object sender,
        BasicDeliverEventArgs args)
    {
        var body = args.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        _logger.LogInformation(
            "Evento recebido: {Json}",
            json);

        try
        {
            var @event = JsonSerializer.Deserialize<SpatialDataReceivedEvent>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (@event is null)
            {
                _logger.LogWarning(
                    "Evento nulo — descartando.");

                await _channel!.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: false);

                return;
            }

            using var scope = _scopeFactory.CreateScope();

            var riskService =
                scope.ServiceProvider.GetRequiredService<IRiskService>();

            await riskService.AssessAsync(@event);

            await _channel!.BasicAckAsync(
                args.DeliveryTag,
                multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar evento.");

            await _channel!.BasicNackAsync(
                args.DeliveryTag,
                multiple: false,
                requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}