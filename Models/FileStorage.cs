﻿namespace CloudProperty.Models
{
    public class FileStorage
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string? Description { get; set; } = string.Empty;
        public string Type { get; set; }
        public string ModelName { get; set; }
        public int ModelId { get; set; }
        public byte Active { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
