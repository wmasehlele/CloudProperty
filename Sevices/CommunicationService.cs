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
        private readonly TemplateService _templateService;

        public CommunicationService(DatabaseContext context, IOptions<MailSettingsService> mailSettings, TemplateService templateService)
        {
            _context = context;
            _mailSettingsService = mailSettings.Value;
            _templateService = templateService;
        }

        public async Task<bool> SendEmail(SendEmailDTO sendEmailDto)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_mailSettingsService.DisplayName, _mailSettingsService.Mail));

            if (sendEmailDto.emailRecipients.Count == 0) { return false; }

            foreach (var recipient in sendEmailDto.emailRecipients)
            {
                if (sendEmailDto.emailRecipients.Count > 1)
                {
                    email.Bcc.Add(new MailboxAddress(recipient.Name, recipient.Email));
                }
                else
                {
                    email.To.Add(new MailboxAddress(recipient.Name, recipient.Email));
                }
            }
            
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
