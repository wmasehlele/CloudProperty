using CloudProperty.Models;

namespace CloudProperty.Sevices
{
    public class UserService
    {
        private readonly DatabaseContext context;

        public UserService(DatabaseContext context)
        {
            this.context = context;
        }

        public async Task<List<UserDTO>> GetAllUsers()
        {
            List<UserDTO> userDto = new List<UserDTO>();
            userDto = await this.context.Users
                    .Select(u => new UserDTO()
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Cellphone = u.Cellphone,
                        EmailVerifiedAt = u.EmailVerifiedAt,
                        CellphoneVerifiedAt = u.CellphoneVerifiedAt,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt
                    }).ToListAsync();
            return userDto;
        }

        public async Task<UserDTO> GetUserById(int Id)
        {
            var userDto = new UserDTO();
            userDto = await this.context.Users
                    .Where(u => u.Id == Id).Select(user => new UserDTO()
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Cellphone = user.Cellphone,
                        EmailVerifiedAt = user.EmailVerifiedAt,
                        CellphoneVerifiedAt = user.CellphoneVerifiedAt,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt
                    }).FirstOrDefaultAsync();
            return userDto;
        }
    }
}
