using Microsoft.AspNetCore.Mvc;
using Wpm.Clinic.Application;

namespace Wpm.Clinic.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultationController : ControllerBase
    {
        private readonly ILogger<ConsultationController> _logger;
        private readonly ClinicApplicationService _clinicApplicationService;

        public ConsultationController(ILogger<ConsultationController> logger,ClinicApplicationService clinicApplicationService)
        {
            _logger = logger;
            _clinicApplicationService = clinicApplicationService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start(StartConsultationCommand command)
        {
            var new_consultation = await _clinicApplicationService.StartConsultation(command);
            return Ok(new_consultation);
        }   
    }
    public record StartConsultationCommand(int PatientId);
}
