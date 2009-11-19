
using System;

using Docky.Services;

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
				try {
					return System.Text.ASCIIEncoding.ASCII.GetString ((byte[]) BusObject.Get (BusName, "Ssid"));
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (ObjectPath);
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return "Unknown SSID";
				}
			}
		}

		public byte Strength {
			get
			{
				try {
					return (byte) BusObject.Get (BusName, "Strength");
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return (byte) 0;
				}
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
