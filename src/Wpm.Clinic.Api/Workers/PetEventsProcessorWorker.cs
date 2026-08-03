using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Wpm.Shared.Events;
using Wpm.Management.Api.DataAccess;

namespace Wpm.Clinic.Api.Workers;

/// <summary>
/// A background service that continuously listens to the 'pet-events' Service Bus topic
/// and keeps the local PetReplicas table in sync with the Management API's pet data.
/// 
/// IMPORTANT: The 'pet-events' topic and 'clinic-api' subscription must be pre-created
/// in the Azure Portal. This service will NOT auto-create them.
/// Required Role on Clinic App's Managed Identity: Azure Service Bus Data Receiver.
/// </summary>
public class PetEventsProcessorWorker : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PetEventsProcessorWorker> _logger;
    private readonly string _topicName;
    private readonly string _subscriptionName;

    // Nullable: only assigned once the processor starts; disposed on stop.
    private ServiceBusProcessor? _processor;

    public PetEventsProcessorWorker(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<PetEventsProcessorWorker> logger,
        IConfiguration configuration)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
        // Read topic/subscription from config so they can be changed without recompiling.
        _topicName = configuration.GetValue<string>("ServiceBus:PetEventsTopic") ?? "pet-events";
        _subscriptionName = configuration.GetValue<string>("ServiceBus:ClinicSubscriptionName") ?? "clinic-api";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Pet Events processor. Topic: {Topic}, Subscription: {Sub}", _topicName, _subscriptionName);

        // The topic and subscription are pre-created in the Azure Portal (no admin client needed).
        _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
        {
            // Process only 1 message at a time to avoid race conditions on the DB upsert.
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false // We control completion (or dead-lettering) manually.
        });

        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        _logger.LogInformation("Received pet event: {Body}", body);

        PetUpdatedEvent? message;
        try
        {
            message = JsonSerializer.Deserialize<PetUpdatedEvent>(body);
        }
        catch (JsonException ex)
        {
            // Poison message: cannot deserialize. Dead-letter it so it doesn't block the queue.
            _logger.LogError(ex, "Failed to deserialize pet event. Dead-lettering message.");
            await args.DeadLetterMessageAsync(args.Message, "DeserializationFailure", ex.Message);
            return;
        }

        if (message == null)
        {
            _logger.LogWarning("Deserialized pet event was null. Dead-lettering message.");
            await args.DeadLetterMessageAsync(args.Message, "NullPayload", "Message deserialized to null.");
            return;
        }

        try
        {
            // IServiceScopeFactory is used here because ClinicDbContext is a scoped service,
            // but this worker is a singleton. We create a short-lived scope per message.
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();

            // Upsert: insert if new, update if already exists.
            var existingPet = await dbContext.PetReplicas.FindAsync(message.Id);
            if (existingPet == null)
            {
                dbContext.PetReplicas.Add(new PetReplica { Id = message.Id, Name = message.Name, Age = message.Age });
            }
            else
            {
                existingPet.Name = message.Name;
                existingPet.Age = message.Age;
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Upserted PetReplica for Id: {Id}", message.Id);

            // Acknowledge the message only on successful processing.
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            // Don't complete the message — let Service Bus retry it based on the subscription's retry policy.
            _logger.LogError(ex, "Failed to process pet event for Id: {Id}. Message will be retried.", message.Id);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processor encountered an infrastructure error. Source: {Source}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stopping Pet Events processor.");
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(stoppingToken);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(stoppingToken);
    }
}

