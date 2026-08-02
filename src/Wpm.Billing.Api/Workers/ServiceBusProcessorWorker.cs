using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Wpm.Shared.Events;
using Wpm.Billing.Api.DataAccess;

namespace Wpm.Billing.Api.Workers;

public class ServiceBusProcessorWorker : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient _adminClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServiceBusProcessorWorker> _logger;
    private ServiceBusProcessor _processor;

    public ServiceBusProcessorWorker(ServiceBusClient client, Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient adminClient, IServiceScopeFactory scopeFactory, ILogger<ServiceBusProcessorWorker> logger)
    {
        _client = client;
        _adminClient = adminClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Service Bus Processor...");
        var topicName = "consultation-events";
        var subscriptionName = "billing-api";
        
        if (!await _adminClient.TopicExistsAsync(topicName, stoppingToken))
        {
            _logger.LogInformation($"Creating Service Bus Topic: {topicName}");
            await _adminClient.CreateTopicAsync(topicName, stoppingToken);
        }

        if (!await _adminClient.SubscriptionExistsAsync(topicName, subscriptionName, stoppingToken))
        {
            _logger.LogInformation($"Creating Service Bus Subscription: {subscriptionName} for Topic: {topicName}");
            await _adminClient.CreateSubscriptionAsync(topicName, subscriptionName, stoppingToken);
        }

        _processor = _client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions());

        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        _logger.LogInformation($"Received message: {body}");

        var message = JsonSerializer.Deserialize<ConsultationStartedEvent>(body);
        if (message != null)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                ConsultationId = message.ConsultationId,
                PatientId = message.PatientId,
                Amount = 50.00m,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Invoices.AddAsync(invoice);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"Generated Invoice {invoice.Id} for Consultation {message.ConsultationId}");
        }

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, $"Message handler encountered an exception.");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stopping Service Bus Processor...");
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(stoppingToken);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(stoppingToken);
    }
}
