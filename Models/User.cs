namespace CloudProperty.Models
{
    public class RefreshToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
    }

    public class User : AppModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cellphone { get; set; } = string.Empty;
        public DateTime? EmailVerifiedAt { get; set; }
        public DateTime? CellphoneVerifiedAt { get; set; }
        public string Password { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenCreatedAt { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        private readonly DatabaseContext context;

        public User() { }

        public User (DatabaseContext  context) {
            this.context = context;
        }

        public async Task<List<User>> GetAllUsers() {
            var user = new List<User>();
            user = await this.context.Users
                    .Select(user => new User() {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Cellphone = user.Cellphone,
                        EmailVerifiedAt = user.EmailVerifiedAt,
                        RefreshToken = user.RefreshToken,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt
                    }).ToListAsync();
            return user;
        }

        public async Task<User> GetUserById(int Id)
        {
            var user = new User();
            user = await this.context.Users
                    .Where(u => u.Id == Id)
                    .Select(user => new User()
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Cellphone = user.Cellphone,
                        EmailVerifiedAt = user.EmailVerifiedAt,
                        RefreshToken = user.RefreshToken,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt
                    }).FirstOrDefaultAsync();
            return user;
        }
    }
}   
