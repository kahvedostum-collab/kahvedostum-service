using System.Text;
using System.Text.Json;
using KahveDostum_Service.Application.Dtos;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace KahveDostum_Service.Infrastructure.Services;

public sealed class RabbitOcrJobPublisher : IOcrJobPublisher
{
    private readonly RabbitOptions _opt;

    public RabbitOcrJobPublisher(IOptions<RabbitOptions> opt)
    {
        _opt = opt.Value;
    }

    public async Task PublishAsync(OcrJobMessage msg, CancellationToken ct = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opt.Host,
            Port = _opt.Port,
            UserName = _opt.User,
            Password = _opt.Pass,
            VirtualHost = _opt.VHost
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken: ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: _opt.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            CorrelationId = msg.JobId,
            Headers = new Dictionary<string, object> { ["attempt"] = 1 }
        };

        await channel.BasicPublishAsync(
            exchange: _opt.Exchange,
            routingKey: _opt.JobsRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}