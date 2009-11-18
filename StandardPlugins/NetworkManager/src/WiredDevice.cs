
using System;
using System.Linq;

using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	
	public class WiredDevice : NetworkDevice
	{
		
		public WiredDevice (string objectPath) : base (objectPath)
		{
			this.WiredProperties = new DBusObject<IWiredDevice> ("org.freedesktop.NetworkManager", objectPath);
		}
		
		public DBusObject<IWiredDevice> WiredProperties { get; private set; }
		
		public bool Carrier {
			get
			{
				return Boolean.Parse (WiredProperties.BusObject.Get (WiredProperties.BusName, "Carrier").ToString ());
			}
		}
		
		public string HWAddresss {
			get
			{
				return WiredProperties.BusObject.Get (WiredProperties.BusName, "HwAddress").ToString ();
			}
		}
	}
}
