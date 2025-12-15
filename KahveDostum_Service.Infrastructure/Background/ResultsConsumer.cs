using System.Text;
using System.Text.Json;
using KahveDostum_Service.Domain.Entities;
using KahveDostum_Service.Infrastructure.Data;
using KahveDostum_Service.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace KahveDostum_Service.Infrastructure.Background;

public sealed class ResultsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitOptions _opt;

    private IConnection? _connection;
    private IChannel? _channel;

    public ResultsConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitOptions> opt)
    {
        _scopeFactory = scopeFactory;
        _opt = opt.Value;
    }

    private sealed class OcrResultMessage
    {
        public string? JobId { get; set; }
        public int ReceiptId { get; set; }
        public string? Status { get; set; }     // DONE / FAILED
        public JsonElement? Payload { get; set; }
        public string? RawText { get; set; }
        public string? Error { get; set; }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opt.Host,
            Port = _opt.Port,
            UserName = _opt.User,
            Password = _opt.Pass,
            VirtualHost = _opt.VHost
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken: stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _opt.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _opt.ResultsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: _opt.ResultsQueue,
            exchange: _opt.Exchange,
            routingKey: _opt.ResultsRoutingKey,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 5, false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<OcrResultMessage>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (msg is null || msg.ReceiptId <= 0)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var receipt = await db.Receipts
                    .Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == msg.ReceiptId, stoppingToken);

                if (receipt == null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
                    return;
                }

                receipt.RawText = msg.RawText;

                if (string.Equals(msg.Status, "DONE", StringComparison.OrdinalIgnoreCase))
                {
                    receipt.Status = ReceiptStatus.DONE;
                    receipt.ProcessedAt = DateTime.UtcNow;
                }
                else
                {
                    receipt.Status = ReceiptStatus.FAILED;
                    receipt.ProcessedAt = DateTime.UtcNow;
                    receipt.RejectReason = msg.Error ?? "OCR FAILED";
                }

                await db.SaveChangesAsync(stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
            }
            catch
            {
                try
                {
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken: stoppingToken);
                }
                catch { }
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _opt.ResultsQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try { if (_channel is not null) await _channel.CloseAsync(cancellationToken: cancellationToken); } catch { }
        try { if (_connection is not null) await _connection.CloseAsync(cancellationToken: cancellationToken); } catch { }

        await base.StopAsync(cancellationToken);
    }
}
