using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MQTTLib
{
	public class PreviousSubscription
	{
		public int cid { get; set; }
		public bool no_local { get; set; }
		public int qos { get; set; }
		public bool rap { get; set; }
		public int retain_handling { get; set; }
		public int subscription_id { get; set; }
		public string topic { get; set; }
	}

	public class SubscriptionList : List<PreviousSubscription> { }
}
