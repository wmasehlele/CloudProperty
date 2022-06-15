using Microsoft.AspNetCore.Mvc;
using CloudProperty.Models;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using System.Security.Authentication;

namespace CloudProperty.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext context;
        private readonly IConfiguration configuration;
        private int authUserId = 0;
        private User user =  new User();
        private LookupToken lookupToken = new LookupToken();
       
        public AuthController() { }

        public AuthController(DataContext context, IConfiguration configuration) { 
            this.configuration = configuration;
            this.context = context;
            user = new User(this.context);
            lookupToken = new LookupToken(this.context);    
        }

        [HttpGet("get-users"), Authorize]
        public async Task<ActionResult<List<User>>> GetUsers() 
        {            
            return Ok(await user.GetAllUsers());
        }

        [HttpPost("request-contact-verification"), Authorize]
        public async Task<ActionResult<string>> RequestContactVerification(UserDto request)
        {
            string contactType = Request.Query["contactType"].ToString();
            var refreshToken = Request.Cookies["refreshToken"];
            user = await this.context.Users.Where(
                u => u.JwtToken == refreshToken && u.JwtTokenExpiresAt > DateTime.UtcNow
            ).FirstOrDefaultAsync();

            if (user == null) { return BadRequest("Invalid credentials"); }

            string otp = "85632";
            HttpContext.Session.SetString("cellphone-verification", otp);

            if (contactType == "email")
            {
                // email the otp
            }

            if (contactType == "cellphone")
            {
                // sms the opt                
            }
            return Ok(otp);
        }

        [HttpPost("contact-verification"), Authorize]
        public async Task<ActionResult<string>> ContactVerification(UserDto request)
        {
            string contactType = Request.Query["contactType"].ToString();
            var refreshToken = Request.Cookies["refreshToken"];
            user = await this.context.Users.Where(
                u => u.JwtToken == refreshToken && u.JwtTokenExpiresAt > DateTime.UtcNow
            ).FirstOrDefaultAsync();

            if (user != null)
            {
                if (contactType == "email")
                {
                    lookupToken.Action = "email-verification";
                    lookupToken.ModelName = "User";
                    lookupToken.ModelData = user.Id.ToString();
                    lookupToken = await lookupToken.CreateLookupToken(lookupToken);
                    string host = this.configuration.GetSection("AppSettings:baseUrl").Value.ToString();
                    string url = host + "api/auth/reset-password?uid=" + lookupToken.Token.ToString();
                    // add function for emailing back to user.
                    return url;
                }

                if (contactType == "cellphone")
                {
                    // cache this code.
                    string otp = "85632";
                    HttpContext.Session.SetString("cellphone-verification", otp);
                    return otp;
                }
            }
            return "";
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request) {
            
            user = await this.context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user != null)
            {
                return BadRequest("User already exists");
            }

            if (request.Password != request.PasswordConfirmation) {
                return BadRequest("Passwords do not match");
            }

            user = new User();
            CreatePasswordHash(request.Password, out string passwordHash);            
            user.Email = request.Email;
            user.Cellphone = request.Cellphone;
            user.Name = request.Name;
            user.Password = passwordHash;
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            try
            {
                this.context.Users.Add(user);
                await this.context.SaveChangesAsync();
                return Ok(user);
            }
            catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDto request) {

            user = await this.context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user == null)
            {
                return BadRequest("Invalid credential");
            }

            if ( (user.Email != request.Email) || !verifyPasswordHash(request.Password, user.Password))
            {
                return BadRequest("Invalid credentials");
            }
            string token = CreateToken(user);
            var refreshToken = GenerateRefreshToken(user);
            await SetRefreshToken(refreshToken);
            user.JwtToken = token;
            return Ok(token);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<User>> RefreshToken() 
        {
            var refreshToken = Request.Cookies["refreshToken"];
            user = await this.context.Users.Where(
                u => u.JwtToken == refreshToken && u.JwtTokenExpiresAt > DateTime.UtcNow
            ).FirstOrDefaultAsync();
            
            if (user == null) {
                return Unauthorized("Invalid or expired refresh token");
            }
            string token = CreateToken(user);
            var newRefreshToken = GenerateRefreshToken(user);
            await SetRefreshToken(newRefreshToken);            
            return Ok(token);
        }

        [HttpPost("request-password-reset")]
        public async Task<ActionResult<string>> RequestPasswordReset(UserDto request)
        {
            user = await this.context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user != null)
            {
                lookupToken.Action = "password-reset";
                lookupToken.ModelName = "User";
                lookupToken.ModelData = user.Id.ToString();
                lookupToken = await lookupToken.CreateLookupToken(lookupToken);

                string host = this.configuration.GetSection("AppSettings:baseUrl").Value.ToString();
                string url = host + "api/auth/reset-password?uid=" + lookupToken.Token.ToString();
                // add function for emailing back to user.
                return url;
            }
            return "";
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<string>> ResetPassword(UserDto request) {

            string token = Request.Query["uid"].ToString();
            lookupToken = await this.context.LookupTokens.Where(t => t.Token == token).FirstOrDefaultAsync();

            if (lookupToken == null) { return BadRequest("Invalid or expired password reset url"); }

            user = await this.context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user == null || user.Id != Convert.ToInt32(lookupToken.ModelData))
            {
                return BadRequest("Password reset failed");
            }

            if (request.Password != request.PasswordConfirmation)
            {
                return BadRequest("Passwords do not match");
            }

            CreatePasswordHash(request.Password, out string passwordHash);
            user.Password = passwordHash;
            user.JwtToken = null;
            user.JwtTokenCreatedAt = null;
            user.JwtTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            try
            {
                await this.context.SaveChangesAsync();
                return Ok( await user.GetUserById(user.Id) );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private RefreshToken GenerateRefreshToken(User user) 
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }; 
            return refreshToken;
        }

        private async Task<bool> SetRefreshToken(RefreshToken newRefreshToken) 
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRefreshToken.ExpiresAt
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);

            user.JwtToken = newRefreshToken.Token;
            user.JwtTokenCreatedAt = newRefreshToken.CreatedAt;
            user.JwtTokenExpiresAt = newRefreshToken.ExpiresAt;
            user.UpdatedAt = newRefreshToken.CreatedAt;
            await this.context.SaveChangesAsync();
            return true;
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),

                //new Claim(ClaimTypes.Role, "Hola")
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(this.configuration.GetSection("AppSettings:secrete").Value));
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
            string salt = this.configuration.GetSection("AppSettings:secrete").Value;
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
        public async Task<UserDTO> GetAuthUser()
        {
            if (!User.Identity.IsAuthenticated)
                throw new AuthenticationException();

            string authUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            this.authUserId = int.Parse(authUserId);

            return await user.GetUserById( this.authUserId );
        }
    }
}
