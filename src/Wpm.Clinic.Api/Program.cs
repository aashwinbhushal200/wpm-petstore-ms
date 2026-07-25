
using Microsoft.EntityFrameworkCore;
using Polly;
using Wpm.Clinic.Application;
using Wpm.Clinic.ExternalServices;
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

            // no need as  AddHttpClient replaces this: builder.Services.AddScoped<ManagementService>();
            builder.Services.AddScoped<ClinicApplicationService>();
            var baseUrl = builder.Configuration.GetValue<string>("WPM:ManagementBaseUrl") ?? "https://localhost:5001";
            builder.Services.AddHttpClient<ManagementService>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            }).AddResilienceHandler("management-pipeline", builder =>
            {
                builder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>()
                {
                    BackoffType = DelayBackoffType.Exponential,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(10)
                });
            }); ;
           

            builder.Services.AddDbContext<ClinicDbContext>(options =>
            { options.UseInMemoryDatabase("WpmClinic"); });

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
