using CloudProperty.Sevices;
using System.Net;
using System.Text.Json;

namespace CloudProperty
{
	public class ErrorDetails
	{
		public int StatusCode { get; set; }
		public string Message { get; set; }

		public override string ToString()
		{
			var options = new JsonSerializerOptions { WriteIndented = true };
			return JsonSerializer.Serialize(this, options);
		}
	}
	public class ExceptionMiddleware : IMiddleware
	{

		private static LoggerService _logger;// = LogManager.GetCurrentClassLogger();

		public ExceptionMiddleware(LoggerService loggerService)
		{
			_logger = loggerService;
		}

		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			try
			{
				await next(context);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex);
				throw;
				//await HandleExceptionAsync(context, ex);
			}
		}

		private Task HandleExceptionAsync(HttpContext context, Exception ex)
		{
			context.Response.ContentType = "application/json";
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			return context.Response.WriteAsync(new ErrorDetails
			{
				StatusCode = context.Response.StatusCode,
				Message = ex.Message
			}.ToString());
		}
	}
}
