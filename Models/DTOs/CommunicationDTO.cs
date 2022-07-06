namespace CloudProperty.Data
{
	public class CommunicationDTO
	{
		public int Id { get; set; }
		public string Type { get; set; }
		public string Status { get; set; }
		public string MessageTrigger { get; set; } // what initiate the comm... ie. user registration
		public int SentBy { get; set; } // logged user who sent the comm
		public string Request { get; set; }
		public string Response { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class SendEmailDTO
	{
		public List<EmailRecipient> emailRecipients { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public List<IFormFile> Attachments { get; set; }
	}

	public class EmailRecipient
	{

		public string Email { get; set; }
		public string Name { get; set; } = string.Empty;

		public EmailRecipient(string email, string name)
		{
			Email = email;
			Name = name;
		}
	}

	public class SendSmsDTO
	{
		public List<SmsRecipient> smsRecipients { get; set; }
		public string Subject { get; set; } = string.Empty;
		public string Message { get; set; }
	}

	public class SmsRecipient
	{
		public string Cellphone { get; set; }
		public string Name { get; set; } = string.Empty;
		public SmsRecipient(string cellphone, string name)
		{
			Cellphone = cellphone;
			Name = name;
		}
	}

	public class ClickaTellMessage
	{
		public string channel { get; set; }
		public string content { get; set; }
		public string to { get; set; }

		//public List<ClickaTellMessage> messages { get; set; }
		//public string From { get; set; }
		//public bool Binary { get; set; }
		//public string ClientMessageId { get; set; }
		//public string ScheduleDeliveryTime { get; set; }
		//public string UserDataHeader { get; set; }
		//public int ValidityPeriod { get; set; }
		//public string Charset { get; set; }
	}
}
