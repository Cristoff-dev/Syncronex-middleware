using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using RabbitMQ.Client;

// Import our custom namespaces
using Syncronex.Api.Features.Webhooks;
using Syncronex.Api.Infrastructure.Data;
using Syncronex.Api.Infrastructure.ExternalServices;

// 1. Early Serilog configuration (Bootstrap Logger)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
  Log.Information("Starting Syncronex API engine...");

  var builder = WebApplication.CreateBuilder(args);

  // 2. Configure Serilog for structured logging
  builder.Host.UseSerilog((context, services, configuration) => configuration
      .ReadFrom.Configuration(context.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext()
      .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

  // =========================================================================
  // 3. DEPENDENCY INJECTION CONTAINER (Services & Infrastructure)
  // =========================================================================

  builder.Services.AddHealthChecks();

  // A. Configure Entity Framework Core with PostgreSQL
  builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

  builder.Services.AddSingleton<IConnection>(sp =>
  {
    var factory = new ConnectionFactory()
    {
      HostName = "localhost",
      UserName = "admin",
      Password = "admin123",
      DispatchConsumersAsync = true
    };
    return factory.CreateConnection();
  });

  // C. Register the Background Consumer (RabbitMQ Worker)
  builder.Services.AddHostedService<WebhookWorker>();

  // D. Register HTTP Clients with Polly Resilience Policies
  // We configure the BaseAddress here so the clients don't need to know the full URLs.
  builder.Services.AddHttpClient<CrmClient>(client =>
  {
    // In a real scenario, fetch this from builder.Configuration
    client.BaseAddress = new Uri("https://api.crm-simulator.com");
  })
  .AddPolicyHandler(ResiliencyPolicies.GetRetryPolicy())
  .AddPolicyHandler(ResiliencyPolicies.GetCircuitBreakerPolicy());

  builder.Services.AddHttpClient<AccountingClient>(client =>
  {
    client.BaseAddress = new Uri("https://api.accounting-simulator.com");
  })
  .AddPolicyHandler(ResiliencyPolicies.GetRetryPolicy())
  .AddPolicyHandler(ResiliencyPolicies.GetCircuitBreakerPolicy());

  // =========================================================================

  var app = builder.Build();

  // 4. HTTP PIPELINE

  app.UseSerilogRequestLogging();
  app.MapHealthChecks("/health");

  // 5. ENDPOINT REGISTRATION
  WebhookEndpoints.Map(app);

  app.Run();
}
catch (HostAbortedException)
{
  // EF Core tooling throws this exception intentionally to stop the web server after reading the DI container.
  // We must re-throw it so the EF Core CLI can finish its job.
  throw;
}
catch (Exception ex)
{
  // If the API crashes on startup for any other reason, it is logged here.
  Log.Fatal(ex, "Critical failure: Syncronex API failed to start");
}
finally
{
  // Ensures all in-memory logs are flushed before the process shuts down
  Log.CloseAndFlush();
}
