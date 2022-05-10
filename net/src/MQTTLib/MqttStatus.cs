using System;
using System.Text;

namespace MQTTLib
{
	public class MqttStatus
	{
		public Guid Key { get; private set; }

		public bool Error { get; private set; } = false;

		public string ErrorMessage { get; private set; }

		public static MqttStatus Success(Guid key)
		{
			return new MqttStatus { Key = key };
		}

		public static MqttStatus Fail(Guid key, Exception ex)
		{
			StringBuilder sb = new StringBuilder(ex.Message);
			Exception inner = ex.InnerException;
			while (inner != null)
			{
				sb.AppendLine(inner.Message);
				inner = inner.InnerException;
			}

			return new MqttStatus { Key = key, Error = true, ErrorMessage = sb.ToString() };
		}
	}
}
