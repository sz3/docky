
using System;

namespace NetworkManagerDocklet
{
	
	
	public class WirelessAccessPoint : DBusObject<IAccessPoint>, IComparable<WirelessAccessPoint>
	{
		
		public WirelessAccessPoint(string objectPath) : base ("org.freedesktop.NetworkManager", objectPath)
		{
		}
		
		public string SSID {
			get
			{
				return System.Text.ASCIIEncoding.ASCII.GetString ((byte[]) BusObject.Get (BusName, "Ssid"));
			}
		}

		public byte Strength {
			get
			{
				return  (byte) BusObject.Get (BusName, "Strength");
			}
		}
		
#region IComparable<WirelessAccessPoint>
		public int CompareTo (WirelessAccessPoint other)
		{
			if (this.Strength >= other.Strength)
				return -1;
			else
				return 1;
		}
#endregion
	}
}
