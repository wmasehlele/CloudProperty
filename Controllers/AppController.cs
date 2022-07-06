using CloudProperty.Models;
using CloudProperty.Sevices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CloudProperty.Controllers
{
    public class AppController : ControllerBase
    {
        protected int AuthUserID => int.Parse(FindClaim(ClaimTypes.NameIdentifier));
        protected DatabaseContext _context;
        protected IConfiguration _configuration;
        protected DataCacheService _dataCacheService;
        protected UserService _userService;

        protected string FindClaim(string claimName)
        {
            try
            {
                var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
                var claim = claimsIdentity.FindFirst(claimName);

                if (claim == null)
                {
                    return null;
                }
                return claim.Value;
            }
            catch {
                return string.Empty;
            }
        }

        protected RefreshToken GenerateRefreshToken(User user)
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            return refreshToken;
        }

        protected async Task<bool> SetRefreshToken(RefreshToken newRefreshToken, User user)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRefreshToken.ExpiresAt
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);

            user.RefreshToken = newRefreshToken.Token;
            user.RefreshTokenCreatedAt = newRefreshToken.CreatedAt;
            user.RefreshTokenExpiresAt = newRefreshToken.ExpiresAt;
            user.UpdatedAt = newRefreshToken.CreatedAt;
            
            await _context.SaveChangesAsync();

            return true;
        }

        protected string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),

                //new Claim(ClaimTypes.Role, "Hola")
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:secrete").Value));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: cred);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        protected void CreatePasswordHash(string password, string passwordSalt, out string passwordHash)
        {
            byte[] pwdSalt = System.Text.Encoding.UTF8.GetBytes(passwordSalt);
            using (var hmac = new HMACSHA512(pwdSalt))
            {
                byte[] computeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                passwordHash = Convert.ToBase64String(computeHash);
            }
        }

        protected bool verifyPasswordHash(string password, string passwordHash, string passwordSalt)
        {
            CreatePasswordHash(password, passwordSalt, out string computedHash);
            return passwordHash == computedHash;
        }

        public int GenerateOtp(int maxRange = 10, int maxDigits = 5)
        {
            string randomNo = String.Empty;
            Random rnd = new Random();
            for (int j = 0; j < 5; j++)
            {
                randomNo = randomNo + rnd.Next(0, 10).ToString();
            }
            return Convert.ToInt32(randomNo);
        }

        public string GeneratePasswordSalt() {
            using (var hmac = new HMACSHA512())
            {
                return Convert.ToBase64String(hmac.Key);
            }
        }
    }
}
