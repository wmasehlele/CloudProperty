namespace CloudProperty.Data
{
    public class LookupTokenDTO
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public string Action { get; set; }
        public string ModelName { get; set; }
        public string ModelData { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
