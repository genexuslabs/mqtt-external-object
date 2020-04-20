using System;
using MQTTnet.Diagnostics;

namespace MQTTLib
{
	class ConsoleLogger : IMqttNetLogger
	{
		public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

		public ConsoleLogger()
		{
			LogMessagePublished += ConsoleLogger_LogMessagePublished;
		}

		private void ConsoleLogger_LogMessagePublished(object sender, MqttNetLogMessagePublishedEventArgs e) => Console.WriteLine($"({e.LogMessage.Source}){e.LogMessage.Level}:{e.LogMessage.Message}");

		public IMqttNetLogger CreateChildLogger(string source)
		{
			Console.WriteLine($"CreateChildLogger:{source}");
			return new ConsoleLogger();
		}

		public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
		{
			Console.WriteLine($"Publish:{logLevel},'{string.Format(message, parameters)}'");
			if (exception != null)
				Console.WriteLine($"Publish ERROR: {exception.Message}");
		}
	}
}
