namespace CloudProperty.Models
{
    public class Template
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }    
        public string Description { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
