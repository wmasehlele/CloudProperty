namespace CloudProperty.Models
{
	public class User : AppModel
	{
		public int Id { get; set; }
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Cellphone { get; set; } = string.Empty;
		public DateTime? EmailVerifiedAt { get; set; }
		public DateTime? CellphoneVerifiedAt { get; set; }
		public string Password { get; set; }
		public string? RefreshToken { get; set; } = string.Empty;
		public DateTime? RefreshTokenCreatedAt { get; set; }
		public DateTime? RefreshTokenExpiresAt { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	}
}
