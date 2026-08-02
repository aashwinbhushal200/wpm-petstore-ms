using Microsoft.Extensions.Caching.Memory;
using Wpm.Clinic.Controllers;
using Wpm.Clinic.ExternalServices;
using Wpm.Management.Api.DataAccess;
using Wpm.Shared.Events;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Wpm.Clinic.Application
{
    public class ClinicApplicationService(ClinicDbContext dbContext, ManagementService managementService, IMemoryCache memoryCache, ServiceBusClient serviceBusClient)
    {
        public async Task<Consultation> StartConsultation(StartConsultationCommand start_command)
        {
            //var petInfo = await managementService.GetPetInfo(start_command.PatientId);
            var petInfo = await memoryCache.GetOrCreateAsync(start_command.PatientId, async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                return await managementService.GetPetInfo(start_command.PatientId);
            });
            var consultation = new Consultation(Guid.NewGuid(), start_command.PatientId, petInfo.Name, petInfo.Age, DateTime.Now);
           
            await dbContext.Consultations.AddAsync(consultation);
            await dbContext.SaveChangesAsync();

            var consultationStartedEvent = new ConsultationStartedEvent
            {
                ConsultationId = consultation.Id,
                PatientId = consultation.PatientId,
                PatientName = consultation.PatientName,
                PatientAge = consultation.PatientAge,
                StartTime = consultation.StartTime
            };

            var sender = serviceBusClient.CreateSender("consultation-events");
            var message = new ServiceBusMessage(JsonSerializer.Serialize(consultationStartedEvent));
            await sender.SendMessageAsync(message);

            return consultation;
        }
    }
}
