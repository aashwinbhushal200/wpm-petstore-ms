using Wpm.Clinic.Controllers;
using Wpm.Clinic.ExternalServices;
using Wpm.Management.Api.DataAccess;

namespace Wpm.Clinic.Application
{
    public class ClinicApplicationService(ClinicDbContext dbContext, ManagementService managementService)
    {
        public async Task<Consultation> StartConsultation(StartConsultationCommand start_command)
        {
            var petInfo = await managementService.GetPetInfo(start_command.PatientId);
            var consultation = new Consultation(Guid.NewGuid(), start_command.PatientId, petInfo.Name, petInfo.Age, DateTime.Now);
            dbContext.Consultations.Add(consultation);
            await dbContext.SaveChangesAsync();
            return consultation;
        }
    }
}
