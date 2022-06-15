using System.Security.Authentication;
using System.Security.Claims;

namespace CloudProperty.Models
{
    public class UserDTO 
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cellphone { get; set; } = string.Empty;
        public DateTime? EmailVerifiedAt { get; set; }
        public DateTime? CellphoneVerifiedAt { get; set; }
        public string? JwtToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cellphone { get; set; } = string.Empty;
        public DateTime? EmailVerifiedAt { get; set; }
        public DateTime? CellphoneVerifiedAt { get; set; }
        public string Password { get; set; }
        public string? JwtToken { get; set; }
        public DateTime? JwtTokenCreatedAt { get; set; }
        public DateTime? JwtTokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        private readonly DataContext context;

        public User() { }

        public User (DataContext  context) {
            this.context = context;
        }

        public async Task<List<UserDTO>> GetAllUsers() {
            var UserDTO = new List<UserDTO>();
            UserDTO = await this.context.Users
                    .Select(UserDTO => new UserDTO() {
                        Id = UserDTO.Id,
                        Name = UserDTO.Name,
                        Email = UserDTO.Email,
                        Cellphone = UserDTO.Cellphone,
                        EmailVerifiedAt = UserDTO.EmailVerifiedAt,
                        JwtToken = UserDTO.JwtToken,
                        CreatedAt = UserDTO.CreatedAt,
                        UpdatedAt = UserDTO.UpdatedAt
                    }).ToListAsync();
            return UserDTO;
        }

        public async Task<UserDTO> GetUserById(int Id)
        {
            var UserDTO = new UserDTO();
            UserDTO = await this.context.Users
                    .Where(u => u.Id == Id)
                    .Select(UserDTO => new UserDTO()
                    {
                        Id = UserDTO.Id,
                        Name = UserDTO.Name,
                        Email = UserDTO.Email,
                        Cellphone = UserDTO.Cellphone,
                        EmailVerifiedAt = UserDTO.EmailVerifiedAt,
                        JwtToken = UserDTO.JwtToken,
                        CreatedAt = UserDTO.CreatedAt,
                        UpdatedAt = UserDTO.UpdatedAt
                    }).FirstOrDefaultAsync();
            return UserDTO;
        }
    }
}   
