using CloudProperty.Models;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CloudProperty.Sevices
{

	public class CommunicationService
	{

		private readonly DatabaseContext _context;
		private readonly MailSettingsService _mailSettingsService;
		private readonly UserService _userService;
		private readonly IHttpClientFactory _httpClientFactory;

		public CommunicationService(
			DatabaseContext context,
			IOptions<MailSettingsService> mailSettings,
			UserService userService,
			IHttpClientFactory httpClientFactory)
		{
			_context = context;
			_mailSettingsService = mailSettings.Value;
			_userService = userService;
			_httpClientFactory = httpClientFactory;

		}

		public async Task<bool> SendEmail(SendEmailDTO sendEmailDto, int authUserId = 0)
		{

			var authUser = new UserDTO();

			if (authUserId > 0)
			{
				authUser = await _userService.GetUserById(authUserId);
			}

			// check env and set default recipient if on test or dev

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

			bool emailSent = false;

			string severRequest = string.Empty;
			string severResponse = string.Empty;

			using (var smtp = new SmtpClient())
			{
				smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
				{
					return true;
				};
				smtp.Connect(_mailSettingsService.Host, _mailSettingsService.Port, true);
				smtp.Authenticate(_mailSettingsService.Mail, _mailSettingsService.Password);
				severResponse = await smtp.SendAsync(email);
				smtp.Disconnect(true);

				emailSent = true;
			}

			var communicationDto = new CommunicationDTO();
			communicationDto.Type = "Email";
			communicationDto.Status = emailSent ? "Sent" : "Failed";
			communicationDto.MessageTrigger = sendEmailDto.Subject;
			communicationDto.SentBy = authUser != null ? authUser.Id : 0;
			communicationDto.Request = severRequest;
			communicationDto.Response = severResponse;
			communicationDto.CreatedAt = DateTime.UtcNow;
			communicationDto.UpdatedAt = DateTime.UtcNow;

			await CreateCommunication(communicationDto);

			return emailSent;
		}

		public async Task<bool> SendSms(SendSmsDTO sendSmsDto, int authUserId = 0)
		{
			var authUser = new UserDTO();

			if (authUserId > 0)
			{
				authUser = await _userService.GetUserById(authUserId);
			}

			// check env and set default recipient if on test or dev

			if (sendSmsDto.smsRecipients.Count == 0) { return false; }

			List<ClickaTellMessage> messages = new List<ClickaTellMessage>();
			foreach (var recipient in sendSmsDto.smsRecipients)
			{
				var clickaTellMessage = new ClickaTellMessage();
				clickaTellMessage.channel = "sms";
				clickaTellMessage.content = sendSmsDto.Message;
				clickaTellMessage.to = "27" + recipient.Cellphone.Substring(1);
				messages.Add(clickaTellMessage);
			}
			Dictionary<string, List<ClickaTellMessage>> data = new Dictionary<string, List<ClickaTellMessage>>();
			data.Add("messages", messages);
			var httpClient = _httpClientFactory.CreateClient("ClickaTell");
			var options = new JsonSerializerOptions { WriteIndented = true };
			var textMessages = JsonSerializer.Serialize(data, options);
			var jsonMessages = new StringContent(
				JsonSerializer.Serialize(data, options),
				Encoding.UTF8,
				Application.Json
			);

			var httpResponseMessage = await httpClient.PostAsync("v1/message", jsonMessages);
			bool smsSent = httpResponseMessage.IsSuccessStatusCode;
			string severRequest = textMessages.ToString();
			string severResponse = await httpResponseMessage.Content.ReadAsStringAsync();
			
			var communicationDto = new CommunicationDTO();
			communicationDto.Type = "Sms";
			communicationDto.Status = smsSent ? "Sent" : "Failed";
			communicationDto.MessageTrigger = sendSmsDto.Subject;
			communicationDto.SentBy = authUser != null ? authUser.Id : 0;
			communicationDto.Request = severRequest;
			communicationDto.Response = severResponse;
			communicationDto.CreatedAt = DateTime.UtcNow;
			communicationDto.UpdatedAt = DateTime.UtcNow;

			await CreateCommunication(communicationDto);

			return smsSent;
		}

		public async Task<bool> CreateCommunication(CommunicationDTO communicationDto)
		{

			if (communicationDto == null)
			{
				return false;
			}

			Communication communication = new Communication();
			communication.Type = communicationDto.Type;
			communication.Status = communicationDto.Status;
			communication.MessageTrigger = communicationDto.MessageTrigger;
			communication.SentBy = communicationDto.SentBy;
			communication.Request = communicationDto.Request;
			communication.Response = communicationDto.Response;
			communication.CreatedAt = communicationDto.CreatedAt;
			communication.UpdatedAt = communicationDto.UpdatedAt;

			_context.Communications.Add(communication);
			await _context.SaveChangesAsync();

			return true;
		}
	}
}
