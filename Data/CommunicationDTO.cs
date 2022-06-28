namespace CloudProperty.Data
{
    public class CommunicationDTO
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string MessageTrigger { get; set; } // what initiate the comm... ie. user registration
        public int SentBy { get; set; } // logged user who sent the comm
        public string ServerResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SendEmailDTO {
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<IFormFile> Attachments { get; set; }
    }

}
