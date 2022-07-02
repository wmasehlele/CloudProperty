using NLog;

namespace CloudProperty.Sevices
{
	public interface ILoggerManager
	{
		void LogInfo(string message);
		void LogWarn(string message);
		void LogDebug(string message);
		void LogError(Exception ex, string message);
	}

	public class LoggerService : ILoggerManager
	{
		private static NLog.ILogger logger = LogManager.GetCurrentClassLogger();

		public void LogDebug(string message) => logger.Debug(message);

		public void LogError(Exception ex, string message = "") => logger.Error(ex, message);

		public void LogInfo(string message) => logger.Info(message);

		public void LogWarn(string message) => logger.Warn(message);
	}
}