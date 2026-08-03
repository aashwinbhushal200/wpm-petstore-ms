using Wpm.Clinic.Controllers;
using Wpm.Management.Api.DataAccess;
using Wpm.Shared.Events;
using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace Wpm.Clinic.Application
{
    public class ClinicApplicationService(ClinicDbContext dbContext, ServiceBusClient serviceBusClient)
    {
        public async Task<Consultation> StartConsultation(StartConsultationCommand start_command)
        {
            var petInfo = await dbContext.PetReplicas.FindAsync(start_command.PatientId);
            
            if (petInfo == null)
            {
                throw new Exception($"Patient with id {start_command.PatientId} not found in local replica.");
            }

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
