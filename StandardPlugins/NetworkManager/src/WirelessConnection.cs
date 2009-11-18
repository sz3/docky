
using System;
using System.Linq;
using System.Collections.Generic;

namespace NetworkManagerDocklet
{
	
	
	public class WirelessConnection : NetworkConnection
	{
		
		public WirelessConnection (string busName, string objectPath, ConnectionOwner owner) : base (busName, objectPath, owner)
		{
		}
		
		public IDictionary<string, object> WirelessProperties {
			get { return Settings["802-11-wireless"]; }
		}
		
		public string SSID {
			get { return System.Text.ASCIIEncoding.ASCII.GetString ((byte[]) WirelessProperties["ssid"]); }
		}
	}
}
