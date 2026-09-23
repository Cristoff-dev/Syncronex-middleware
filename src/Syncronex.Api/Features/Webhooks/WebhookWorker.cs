using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Syncronex.Api.Infrastructure.Data;
using Syncronex.Api.Infrastructure.ExternalServices;

namespace Syncronex.Api.Features.Webhooks;

public class WebhookWorker : BackgroundService
{
  private readonly ILogger<WebhookWorker> _logger;
  private readonly IConnection _mqConnection;
  private readonly IServiceProvider _serviceProvider;

  private IModel _channel = null!;

  public WebhookWorker(
      ILogger<WebhookWorker> logger,
      IConnection mqConnection,
      IServiceProvider serviceProvider)
  {
    _logger = logger;
    _mqConnection = mqConnection;
    _serviceProvider = serviceProvider;
  }

  public override Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Initializing RabbitMQ Consumer Channel...");
    _channel = _mqConnection.CreateModel();

    _channel.QueueDeclare(
        queue: "webhooks_queue",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

    return base.StartAsync(cancellationToken);
  }

  protected override Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var consumer = new AsyncEventingBasicConsumer(_channel);

    consumer.Received += async (model, ea) =>
    {
      var body = ea.Body.ToArray();
      var jsonString = Encoding.UTF8.GetString(body);

      try
      {
        var payload = JsonSerializer.Deserialize<ShopifyWebhookPayload>(jsonString);

        if (payload?.Data?.Customer == null)
        {
          _logger.LogWarning("Received payload is null or missing essential data. Skipping.");
          _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
          return;
        }

        _logger.LogInformation("Processing webhook {EventId} from queue.", payload.EventId);

        using var scope = _serviceProvider.CreateScope();
        var crmClient = scope.ServiceProvider.GetRequiredService<CrmClient>();

        var crmPayload = new CrmUpsertPayload(
            new CrmContact(
                payload.Data.Customer.Email,
                payload.Data.Customer.FirstName,
                payload.Data.Customer.LastName ?? string.Empty,
                payload.Data.Customer.Phone ?? string.Empty),
            new CrmTransaction(
                payload.Data.OrderId ?? string.Empty,
                payload.Data.TotalAmount,
                payload.Data.Currency ?? "USD",
                payload.Timestamp)
        );

        await crmClient.UpsertContactAsync(crmPayload, stoppingToken);

        _logger.LogInformation("Successfully dispatched webhook {EventId} to external services.", payload.EventId);

        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to process message. Moving to Dead Letter DB.");

        await HandleDeadLetterAsync(jsonString, ex.Message, stoppingToken);
        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
      }
    };

    _channel.BasicConsume(queue: "webhooks_queue", autoAck: false, consumer: consumer);

    return Task.CompletedTask;
  }

  private async Task HandleDeadLetterAsync(string rawPayload, string error, CancellationToken token)
  {
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var deadLetter = new DeadLetterRecord
    {
      SourceEventId = ExtractEventIdFast(rawPayload),
      Payload = rawPayload,
      FailureReason = error
    };

    dbContext.DeadLetters.Add(deadLetter);
    await dbContext.SaveChangesAsync(token);
  }

  private string ExtractEventIdFast(string rawPayload)
  {
    try
    {
      using var document = JsonDocument.Parse(rawPayload);
      return document.RootElement.GetProperty("event_id").GetString() ?? "Unknown";
    }
    catch
    {
      return "Parse_Error";
    }
  }

  public override void Dispose()
  {
    _channel?.Dispose();
    GC.SuppressFinalize(this);
    base.Dispose();
  }
}
