using CloudProperty.Sevices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudProperty.Controllers
{
    [Route("api/communication")]
    [ApiController, Authorize]
    public class CommunicationController : ControllerBase
    {
        private readonly CommunicationService _communicationService;

        public CommunicationController(CommunicationService communicationService) {
            _communicationService = communicationService;
        }

        [HttpPost("send-email")]
        public async Task<ActionResult<string>> SendEmail(List<IFormFile> attachments, IFormCollection fileForm) {

            SendEmailDTO sendEmailDto = new SendEmailDTO();
            sendEmailDto.ToEmail = fileForm["ToEmail"];
            sendEmailDto.Subject = fileForm["Subject"];
            sendEmailDto.Body = fileForm["Body"];
            sendEmailDto.Attachments = attachments;

            bool sent = await _communicationService.SendEmailAsync(sendEmailDto);
            if (!sent)
            {
                // log here that email failed to sent....
                return "Email failed";
            }
            
            return "Email sent";
        }
    }
}
