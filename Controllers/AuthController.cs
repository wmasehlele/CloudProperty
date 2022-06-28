using Microsoft.AspNetCore.Mvc;
using CloudProperty.Models;
using Microsoft.AspNetCore.Authorization;
using CloudProperty.Sevices;

namespace CloudProperty.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : AppController
    {
        private LookupTokenService _lookupTokenService;
        private CommunicationService _communicationService;

        private LookupTokenDTO lookupTokenDto;
        private UserDTO userDto;
       
        public AuthController(
            DatabaseContext context, 
            IConfiguration configuration, 
            DataCacheService dataCacheService, 
            UserService userService, 
            LookupTokenService lookupTokenService,
            CommunicationService communicationService) 
        {
            
            _context = context;
            _configuration = configuration;
            _dataCacheService = dataCacheService;
            _userService = userService;
            _lookupTokenService = lookupTokenService;
            _communicationService = communicationService;

            userDto = new UserDTO();
            lookupTokenDto = new LookupTokenDTO();
        }

        [HttpGet("get-users"), Authorize]
        public async Task<ActionResult<List<UserDTO>>> GetUsers() 
        {            
            return Ok(await _userService.GetAllUsers());
        }

        [HttpGet("request-contact-verification"), Authorize]
        public async Task<ActionResult<string>> RequestContactVerification()
        {
            string contactType = Request.Query["contactType"].ToString();
            userDto = await _userService.GetUserById(AuthUserID);
            if (userDto == null) { return Unauthorized(); }

            int otp = GenerateOtp(10,5);
            string cacheKey = userDto.Id.ToString() + "-contact-verification";
            _dataCacheService.SetCacheValue(cacheKey, otp.ToString());

            if (contactType == "email")
            {
                // email the otp
            }

            if (contactType == "cellphone")
            {
                // sms the opt
            }
            return Ok(await _dataCacheService.GetCachedValue(cacheKey));
        }

        [HttpGet("contact-verification/{userOtp}"), Authorize]
        public async Task<ActionResult<string>> ContactVerification(int userOtp)
        {
            userDto = await _userService.GetUserById(AuthUserID);
            if (userDto == null) { return Unauthorized(); }
            string cacheKey = userDto.Id.ToString() + "-contact-verification";

            int opt = Convert.ToInt32(await _dataCacheService.GetCachedValue(cacheKey));
            if (userOtp != opt) {
                return BadRequest("Invalid or expired Otp");
            }
            return Ok(opt);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(UserDTO request) {

            if (String.IsNullOrEmpty(request.Password) || String.IsNullOrEmpty(request.Email)) 
            {
                return BadRequest("Missing information for user registration");
            }

            var user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();
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
            user.CreatedAt = request.CreatedAt;
            user.UpdatedAt = request.UpdatedAt;
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // send welcome email...
                SendEmailDTO sendEmailDto = new SendEmailDTO();
                sendEmailDto.ToEmail = user.Email;
                sendEmailDto.Subject = "Welcome to cloudproperty";
                sendEmailDto.Body = "Dear Client. Welcome to the best rental property management community. attached is your welcome guide.";

                bool sent = await _communicationService.SendEmailAsync(sendEmailDto);
                if (!sent) { 
                    // log here that email failed to sent....
                }




                return Ok(await _userService.GetUserById(user.Id));
            }
            catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDTO request) {

            var user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

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
            var user = await _context.Users.Where(
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
        public async Task<ActionResult<string>> RequestPasswordReset(UserDTO request)
        {
            var user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

            if (user != null)
            {
                var lookupToken = new LookupToken();
                lookupToken.Action = "password-reset";
                lookupToken.ModelName = "User";
                lookupToken.ModelData = user.Id.ToString();
                lookupToken = await _lookupTokenService.CreateLookupToken(lookupToken);

                string host = _configuration.GetSection("AppSettings:baseUrl").Value.ToString();
                string url = host + "api/auth/reset-password?uid=" + lookupToken.Token.ToString();
                // add function for emailing back to user.
                return url;
            }
            return "";
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<string>> ResetPassword(UserDTO request) {

            if (String.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Missing information for password reset");
            }

            string token = Request.Query["uid"].ToString();
            var lookupToken = await _context.LookupTokens.Where(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow ).FirstOrDefaultAsync();

            if (lookupToken == null) { return BadRequest("Invalid or expired password reset url"); }

            userDto = await _userService.GetUserById(Convert.ToInt32(lookupToken.ModelData));

            if (userDto == null)
            {
                return BadRequest("Password reset failed");
            }

            var user = await _context.Users.FindAsync(userDto.Id);
            CreatePasswordHash(request.Password, out string passwordHash);
            user.Password = passwordHash;
            user.RefreshToken = null;
            user.RefreshTokenCreatedAt = null;
            user.RefreshTokenExpiresAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _context.SaveChangesAsync();
                return Ok( await _userService.GetUserById(user.Id) );
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
