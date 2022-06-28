using CloudProperty.Models;

namespace CloudProperty.Sevices
{
    public class TemplateService
    {
        private readonly DatabaseContext _context;
        private readonly MailSettingsService _mailSettingsService;

        public TemplateService(DatabaseContext context)
        {
            _context = context;
        }


        private async Task<string> GetEmailTemplate(int templateId) {

            var template = await _context.Templates.FindAsync(templateId);

            if (template == null) { return string.Empty; }

            string FilePath = Directory.GetCurrentDirectory() + "\\Emails\\";// MainTemplate.html";

            string mainTemplate = File.ReadAllText(FilePath + "MainTemplate.html");
            string mailTemplate = File.ReadAllText(FilePath + template.FileName);
            mainTemplate = string.Format(mainTemplate, mailTemplate);

            return mainTemplate;
        }

        public async Task<string> GetWelcomeMailTemplate(int templateId, UserDTO userDto) {

            string template = await GetEmailTemplate(templateId);

            string emailBody = string.Format(template, userDto.Name);

            return emailBody;
        }

        public async Task<string> GetEmailVerificationMailTemplate(int templateId, UserDTO userDto, int otp) {

            string template = await GetEmailTemplate(templateId);

            string emailBody = string.Format(template, userDto.Name, otp.ToString());

            return emailBody;
        }

        public async Task<string> GetPasswordResetMailTemplate(int templateId, User user, string url) {
            
            string template = await GetEmailTemplate(templateId);

            string emailBody = string.Format(template, user.Name, url, url);

            return emailBody;

        }

        public async Task<string> GetPasswordUpdatedMailTemplate(int templateId, User user) {

            string template = await GetEmailTemplate(templateId);

            string emailBody = string.Format(template, user.Name);

            return emailBody;
        }
    }
}
