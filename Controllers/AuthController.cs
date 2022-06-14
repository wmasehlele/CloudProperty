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
        public static User user = new User();

        IConfiguration configuration = null;

        public AuthController(IConfiguration configuration) { 
            this.configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request) {
            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
            user.Email = request.Email;
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            return Ok(user);    
        
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto request) {

            if (user.Email != request.Email)
            {
                return BadRequest("User not found");
            }

            if (!verifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt)) {
                return BadRequest("Wrong password");
            }

            string token = CreateToken(user);
            return Ok(token);

        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email)
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


        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));   
            }
        }

        private bool verifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt) 
        {
            using (var hmac = new HMACSHA512(passwordSalt)) {
                var computeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computeHash.SequenceEqual(passwordHash);
            }        
        }
       
    }
}
