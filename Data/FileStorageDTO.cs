namespace CloudProperty.Data
{
    public class FileStorageDTO
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string ModelName { get; set; }
        public int ModelId { get; set; }
        public byte Active { get; set; }
        public string FileUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
