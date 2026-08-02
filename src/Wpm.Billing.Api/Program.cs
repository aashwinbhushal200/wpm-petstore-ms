using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Wpm.Billing.Api.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<BillingDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("BillingDb"), sqlOptions => sqlOptions.EnableRetryOnFailure());
});

builder.Services.AddSingleton<Azure.Messaging.ServiceBus.ServiceBusClient>(sp =>
{
    var fullyQualifiedNamespace = builder.Configuration.GetValue<string>("ServiceBusNamespace");
    return new Azure.Messaging.ServiceBus.ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
});

builder.Services.AddSingleton<Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient>(sp =>
{
    var fullyQualifiedNamespace = builder.Configuration.GetValue<string>("ServiceBusNamespace");
    return new Azure.Messaging.ServiceBus.Administration.ServiceBusAdministrationClient(fullyQualifiedNamespace, new DefaultAzureCredential());
});

builder.Services.AddHostedService<Wpm.Billing.Api.Workers.ServiceBusProcessorWorker>();

var app = builder.Build();
app.EnsureBillingDbCreated();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
