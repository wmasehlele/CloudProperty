namespace CloudProperty.Models
{
    public class Communication
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
}
