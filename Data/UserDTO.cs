namespace CloudProperty.Data
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cellphone { get; set; } = string.Empty;
        public DateTime? EmailVerifiedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CellphoneVerifiedAt { get; set; } = DateTime.UtcNow;
        public string? Password { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
