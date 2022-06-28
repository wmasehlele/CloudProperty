using CloudProperty.Models;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CloudProperty.Sevices
{
    public class CommunicationService
    {

        private readonly DatabaseContext _context;
        private readonly MailSettingsService _mailSettingsService;

        public CommunicationService(DatabaseContext context, IOptions<MailSettingsService> mailSettings)
        {
            _context = context;
            _mailSettingsService = mailSettings.Value;
        }

        public async Task<bool> SendEmailAsync(SendEmailDTO sendEmailDto)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_mailSettingsService.DisplayName, _mailSettingsService.Mail));
            email.To.Add(new MailboxAddress(null, sendEmailDto.ToEmail));
            email.Subject = sendEmailDto.Subject;
            
            var builder = new BodyBuilder();
            if (sendEmailDto.Attachments != null)
            {
                byte[] fileBytes;
                foreach (var file in sendEmailDto.Attachments)
                {
                    if (file.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            file.CopyTo(ms);
                            fileBytes = ms.ToArray();
                        }
                        builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
                    }
                }
            }

            builder.HtmlBody = sendEmailDto.Body;
            email.Body = builder.ToMessageBody();

            try
            {
                using (var smtp = new SmtpClient())
                {
                    smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        return true;
                    };
                    smtp.Connect(_mailSettingsService.Host, _mailSettingsService.Port, true);
                    smtp.Authenticate(_mailSettingsService.Mail, _mailSettingsService.Password);
                    string severResponse = await smtp.SendAsync(email);
                    smtp.Disconnect(true);
                }
                return true;
            }
            catch (Exception ex) {
                return false;
            }
        }

        public async Task<bool> CreateCommunication(Communication communicationDto) {
            Communication communication = new Communication();
            communication.Id = communicationDto.Id;
            communication.Type = communicationDto.Type;
            communication.Status = communicationDto.Status;
            communication.MessageTrigger = communicationDto.MessageTrigger;
            communication.SentBy = communicationDto.SentBy;
            communication.CreatedAt = communicationDto.CreatedAt;
            communication.UpdatedAt = communicationDto.UpdatedAt;
            _context.Communications.Add(communication);
            await _context.SaveChangesAsync();
            return true;
        }



    }
}
