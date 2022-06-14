using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CloudProperty.Models;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace CloudProperty.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext context;

        public static User user = new User();

        IConfiguration configuration = null;

        public AuthController(DataContext context, IConfiguration configuration) { 
            this.configuration = configuration;
            this.context = context;
        }

        [HttpGet("get-users")]
        public async Task<ActionResult<List<User>>> GetAllUsers() 
        {
            return Ok(await this.context.Users.ToListAsync());
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request) {
            CreatePasswordHash(request.Password, out string passwordHash);
            user.Email = request.Email;
            user.Cellphone = request.Cellphone; 
            user.Name = request.Name;
            user.Password = passwordHash;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            this.context.Users.Add(user);
            await this.context.SaveChangesAsync();
            return Ok(await this.context.Users.ToListAsync());       
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto request) {
            user = await this.context.Users.Where(u => u.Email == request.Email).FirstAsync();
            if ( (user.Email != request.Email) || !verifyPasswordHash(request.Password, user.Password))
            {
                return BadRequest("Invalid credentials");
            }
            string token = CreateToken(user);
            user.Password = token;
            return Ok(user);
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, "Hola")
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(this.configuration.GetSection("AppSettings:jwt-secrete").Value));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims, 
                expires: DateTime.UtcNow.AddHours(1), 
                signingCredentials: cred );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);  
            return jwt;
        }

        private void CreatePasswordHash(string password, out string passwordHash)
        {
            string salt = this.configuration.GetSection("AppSettings:jwt-secrete").Value;
            byte[] passwordSalt = System.Text.Encoding.UTF8.GetBytes(salt);
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                byte[] computeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                passwordHash = Convert.ToBase64String(computeHash);
            }
        }

        private bool verifyPasswordHash(string password, string passwordHash) 
        {
            CreatePasswordHash(password, out string computedHash);
            return passwordHash == computedHash;
        }       
    }
}
