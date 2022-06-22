using Microsoft.AspNetCore.Mvc;
using CloudProperty.Models;
using Microsoft.AspNetCore.Authorization;

namespace CloudProperty.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : AppController
    {
        private User user =  new User();
        private LookupToken lookupToken = new LookupToken();
       
        public AuthController(DatabaseContext context, IConfiguration configuration, DataCache redisCache) {
            
            _context = context;
            _configuration = configuration;
            _dataCache = redisCache;

            user = new User(_context);
            lookupToken = new LookupToken(_context);    
        }

        [HttpGet("get-users"), Authorize]
        public async Task<ActionResult<List<User>>> GetUsers() 
        {            
            return Ok(await user.GetAllUsers());
        }

        [HttpGet("request-contact-verification/{userId}"), Authorize]
        public async Task<ActionResult<string>> RequestContactVerification(int userId)
        {
            string contactType = Request.Query["contactType"].ToString();

            user = await this.user.GetUserById(AuthUserID);

            if (user == null) { return Unauthorized(); }

            int otp = user.GenerateOtp(10,5);
            string cacheKey = user.Id.ToString() + "-contact-verification";
            _dataCache.SetCacheValue(cacheKey, otp.ToString());

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

        [HttpPost("contact-verification/{userId}/{userOpt}"), Authorize]
        public async Task<ActionResult<string>> ContactVerification(int userId, int userOpt)
        {
            user = await this.user.GetUserById(AuthUserID);

            if (user == null) { return Unauthorized(); }

            string cacheKey = user.Id.ToString() + "-contact-verification";
            int opt = Convert.ToInt32(await _dataCache.GetCachedValue(cacheKey));
            if (userOpt != opt) {
                return BadRequest("Invalid or expired Otp");
            }
            return Ok(opt);
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(User request) {

            if (String.IsNullOrEmpty(request.Password) || String.IsNullOrEmpty(request.Email)) 
            {
                return BadRequest("Missing information for user registration");
            }

            user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user != null)
            {
                return BadRequest("User already exists");
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
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return Ok(user);
            }
            catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(User request) {

            user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

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
            await SetRefreshToken(refreshToken, user);
            user.RefreshToken = token;
            return Ok(token);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<User>> RefreshToken() 
        {
            var refreshToken = Request.Cookies["refreshToken"];
            user = await _context.Users.Where(
                u => u.RefreshToken == refreshToken && u.RefreshTokenExpiresAt > DateTime.UtcNow
            ).FirstOrDefaultAsync();
            
            if (user == null) {
                return Unauthorized("Invalid or expired refresh token");
            }
            string token = CreateToken(user);
            var newRefreshToken = GenerateRefreshToken(user);
            await SetRefreshToken(newRefreshToken, user);            
            return Ok(token);
        }

        [HttpPost("request-password-reset")]
        public async Task<ActionResult<string>> RequestPasswordReset(User request)
        {
            user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user != null)
            {
                lookupToken.Action = "password-reset";
                lookupToken.ModelName = "User";
                lookupToken.ModelData = user.Id.ToString();
                lookupToken = await lookupToken.CreateLookupToken(lookupToken);

                string host = _configuration.GetSection("AppSettings:baseUrl").Value.ToString();
                string url = host + "api/auth/reset-password?uid=" + lookupToken.Token.ToString();
                // add function for emailing back to user.
                return url;
            }
            return "";
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<string>> ResetPassword(User request) {

            if (String.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Missing information for password reset");
            }

            string token = Request.Query["uid"].ToString();
            lookupToken = await _context.LookupTokens.Where(t => t.Token == token && t.ExpiresAt < DateTime.UtcNow ).FirstOrDefaultAsync();

            if (lookupToken == null) { return BadRequest("Invalid or expired password reset url"); }

            user = await user.GetUserById(Convert.ToInt32(lookupToken.ModelData));

            if (user == null)
            {
                return BadRequest("Password reset failed");
            }

            CreatePasswordHash(request.Password, out string passwordHash);
            user.Password = passwordHash;
            user.RefreshToken = null;
            user.RefreshTokenCreatedAt = null;
            user.RefreshTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _context.SaveChangesAsync();
                return Ok( await user.GetUserById(user.Id) );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
