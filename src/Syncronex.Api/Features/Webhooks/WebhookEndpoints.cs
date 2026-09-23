using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Syncronex.Api.Features.Webhooks;

public static class WebhookEndpoints
{
  public static void Map(IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/api/webhooks").WithTags("Webhooks Intake");

    group.MapPost("/receive", (
        ShopifyWebhookPayload payload,
        IConnection mqConnection,
        ILogger<ShopifyWebhookPayload> logger) =>
    {
      // 1. Fast Validation (Fail-Fast pattern)
      if (string.IsNullOrWhiteSpace(payload.EventId) || payload.Data == null)
      {
        logger.LogWarning("Invalid webhook payload received. Missing EventId or Data.");
        return Results.BadRequest(new { error = "Invalid payload structure. EventId and Data are required." });
      }

      logger.LogInformation("Received webhook {EventId} for Order {OrderId}", payload.EventId, payload.Data.OrderId);

      // 2. Publish to RabbitMQ
      using var channel = mqConnection.CreateModel();

      channel.QueueDeclare(
          queue: "webhooks_queue",
          durable: true,
          exclusive: false,
          autoDelete: false,
          arguments: null);

      var jsonString = JsonSerializer.Serialize(payload);
      var body = Encoding.UTF8.GetBytes(jsonString);
      var properties = channel.CreateBasicProperties();
      properties.Persistent = true;

      channel.BasicPublish(
          exchange: "",
          routingKey: "webhooks_queue",
          basicProperties: properties,
          body: body);

      logger.LogInformation("Payload {EventId} successfully queued for background processing.", payload.EventId);

      // 3. Acknowledge receipt immediately (HTTP 202) to prevent blocking the source
      return Results.Accepted();
    });
  }
}
