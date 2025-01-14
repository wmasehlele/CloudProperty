﻿using CloudProperty.Models;
using CloudProperty.Sevices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudProperty.Controllers
{
	[Route("api/auth")]
	[ApiController]
	public class AuthController : AppController
	{
		private readonly ILogger<AuthController> _logger;
		private LookupTokenService _lookupTokenService;
		private CommunicationService _communicationService;
		private TemplateService _templateService;

		private UserDTO userDto;

		public AuthController(
			ILogger<AuthController> logger,
			DatabaseContext context,
			IConfiguration configuration,
			DataCacheService dataCacheService,
			UserService userService,
			LookupTokenService lookupTokenService,
			CommunicationService communicationService,
			TemplateService templateService)
		{
			_logger = logger;
			_context = context;
			_configuration = configuration;
			_dataCacheService = dataCacheService;
			_userService = userService;
			_lookupTokenService = lookupTokenService;
			_communicationService = communicationService;
			_templateService = templateService;

			userDto = new UserDTO();
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

			_logger.LogError("Application with error");

			int otp = GenerateOtp(10, 5);
			string cacheKey = userDto.Id.ToString() + "-contact-verification";

			_dataCacheService.SetCacheValue(cacheKey, otp.ToString());

			if (contactType == "email")
			{
				// email the otp
				SendEmailDTO sendEmailDto = new SendEmailDTO();
				sendEmailDto.emailRecipients = new List<EmailRecipient> {
					new EmailRecipient(userDto.Email, userDto.GetUserFullname())
				};
				sendEmailDto.Subject = "Email verification code";
				int templateId = 2;
				sendEmailDto.Body = await _templateService.GetEmailVerificationMailTemplate(templateId, userDto, otp);

				bool sent = await _communicationService.SendEmail(sendEmailDto, AuthUserID);
				if (!sent)
				{
					// do something
				}
			}

			if (contactType == "cellphone")
			{
				// sms the opt
				SendSmsDTO sendSmsDto = new SendSmsDTO();
				sendSmsDto.smsRecipients = new List<SmsRecipient> {
					new SmsRecipient(userDto.Cellphone, userDto.GetUserFullname())
				};
				var smsTemplate = await _context.Templates.FindAsync(5); // Cellphone verification
				sendSmsDto.Subject = "Cellphone verification code";
				sendSmsDto.Message = string.Format(smsTemplate.Content, otp);

				bool sent = await _communicationService.SendSms(sendSmsDto, AuthUserID);
				if (!sent)
				{
					// do something
				}
			}
			return Ok( await _dataCacheService.GetCachedValue(cacheKey) );
		}

		[HttpGet("contact-verification/{userOtp}"), Authorize]
		public async Task<ActionResult<string>> ContactVerification(int userOtp)
		{
			string contactType = Request.Query["contactType"].ToString();

			userDto = await _userService.GetUserById(AuthUserID);
			if (userDto == null) { return Unauthorized(); }
			string cacheKey = userDto.Id.ToString() + "-contact-verification";
			string cachedValue = await _dataCacheService.GetCachedValue(cacheKey);
			int opt = Convert.ToInt32(cachedValue);
			if (userOtp != opt)
			{
				throw new Exception("Invalid or expired Otp");
				//return BadRequest("Invalid or expired Otp");
			}

			var user = await _context.Users.FindAsync(AuthUserID);
			if (contactType == "email")
			{
				user.EmailVerifiedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}

			if (contactType == "cellphone")
			{
				user.CellphoneVerifiedAt = DateTime.UtcNow;
				await _context.SaveChangesAsync();
			}

			return Ok(opt);
		}

		[HttpPost("register")]
		public async Task<ActionResult<UserDTO>> Register(UserDTO request)
		{

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
			string passwordSalt = GeneratePasswordSalt();
			CreatePasswordHash(request.Password, passwordSalt, out string passwordHash);
			user.Email = request.Email;
			user.Cellphone = request.Cellphone;
			user.FirstName = request.FirstName;
			user.LastName = request.LastName;
			user.Password = passwordHash;
			user.PasswordSalt = passwordSalt;
			user.CreatedAt = request.CreatedAt;
			user.UpdatedAt = request.UpdatedAt;
			try
			{
				_context.Users.Add(user);
				await _context.SaveChangesAsync();

				// send welcome email...                
				SendEmailDTO sendEmailDto = new SendEmailDTO();
				sendEmailDto.emailRecipients = new List<EmailRecipient> {
					new EmailRecipient(user.Email, user.GetUserFullname() )
				};
				sendEmailDto.Subject = "Welcome to cloudproperty";
				int templateId = 1;
				sendEmailDto.Body = await _templateService.GetWelcomeMailTemplate(templateId, request);

				bool sent = await _communicationService.SendEmail(sendEmailDto, user.Id);
				if (!sent)
				{
					// log here that email failed to sent....
				}

				return Ok(await _userService.GetUserById(user.Id));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPost("login")]
		public async Task<ActionResult<string>> Login(UserDTO request)
		{

			var user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();

			if (user == null)
			{
				return BadRequest("Invalid credential");
			}

			if ((user.Email != request.Email) || !verifyPasswordHash(request.Password, user.Password, user.PasswordSalt))
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

			if (user == null)
			{
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

				// send welcome email...                
				SendEmailDTO sendEmailDto = new SendEmailDTO();
				sendEmailDto.emailRecipients = new List<EmailRecipient> {
					new EmailRecipient(user.Email, user.GetUserFullname() )
				};
				sendEmailDto.Subject = "Request for password rest";
				int templateId = 3;
				sendEmailDto.Body = await _templateService.GetPasswordResetMailTemplate(templateId, user, url);

				bool sent = await _communicationService.SendEmail(sendEmailDto, user.Id);
				if (!sent)
				{
					// log here that email failed to sent....
				}
				return url;
			}
			return "";
		}

		[HttpPost("reset-password")]
		public async Task<ActionResult<string>> ResetPassword(UserDTO request)
		{

			if (String.IsNullOrEmpty(request.Password))
			{
				return BadRequest("Missing information for password reset");
			}

			string token = Request.Query["uid"].ToString();
			var lookupToken = await _context.LookupTokens.Where(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow).FirstOrDefaultAsync();

			if (lookupToken == null) { return BadRequest("Invalid or expired password reset url"); }

			userDto = await _userService.GetUserById(Convert.ToInt32(lookupToken.ModelData));

			if (userDto == null)
			{
				return BadRequest("Password reset failed");
			}

			var user = await _context.Users.FindAsync(userDto.Id);
			string passwordSalt = GeneratePasswordSalt();
			CreatePasswordHash(request.Password, passwordSalt, out string passwordHash);
			user.Password = passwordHash;
			user.PasswordSalt = passwordSalt;
			user.RefreshToken = null;
			user.RefreshTokenCreatedAt = null;
			user.RefreshTokenExpiresAt = null;
			user.UpdatedAt = DateTime.UtcNow;
			try
			{
				await _context.SaveChangesAsync();

				// send welcome email...                
				SendEmailDTO sendEmailDto = new SendEmailDTO();
				sendEmailDto.emailRecipients = new List<EmailRecipient> {
					new EmailRecipient(user.Email, user.GetUserFullname())
				};
				sendEmailDto.Subject = "Password updated";
				int templateId = 4;
				sendEmailDto.Body = await _templateService.GetPasswordUpdatedMailTemplate(templateId, user);

				bool sent = await _communicationService.SendEmail(sendEmailDto, user.Id);
				if (!sent)
				{
					// log here that email failed to sent....
				}

				return Ok(await _userService.GetUserById(user.Id));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}
