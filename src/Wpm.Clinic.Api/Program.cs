using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Polly;
using Wpm.Clinic.Application;
using Wpm.Management.Api.DataAccess;

namespace Wpm.Clinic.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMemoryCache();

            builder.Services.AddScoped<ClinicApplicationService>();
            
            builder.Services.AddDbContext<ClinicDbContext>(options =>
            { options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicDb"), sqlOptions => sqlOptions.EnableRetryOnFailure()); });

            builder.Services.AddSingleton<Azure.Messaging.ServiceBus.ServiceBusClient>(sp =>
            {
                var fullyQualifiedNamespace = builder.Configuration.GetValue<string>("ServiceBusNamespace");
                return new Azure.Messaging.ServiceBus.ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
            });

            builder.Services.AddHostedService<Wpm.Clinic.Api.Workers.PetEventsProcessorWorker>();

            var app = builder.Build();
            app.EnsureClinicDbCreated();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
