
using System;
using System.Linq;
using System.Collections.Generic;

using NDesk.DBus;

namespace NetworkManagerDocklet
{
	
	public class DeviceManager : DBusObject<INetManager>
	{
		
		public DeviceManager() : base ("org.freedesktop.NetworkManager", "/org/freedesktop/NetworkManager")
		{
			NetworkDevices = new List<NetworkDevice> ();
			
			foreach (ObjectPath objPath in BusObject.GetDevices ())
			{
				NetworkDevice device = new NetworkDevice (objPath.ToString ());
				if (device.DType == DeviceType.Wired)
					device = new WiredDevice (device.ObjectPath.ToString ());
				else if (device.DType == DeviceType.Wireless)
					device = new WirelessDevice (device.ObjectPath.ToString ());
				else
					continue;
				device.StateChanged += OnStateChanged;
				NetworkDevices.Add (device);
			}
		}
		
		public List<NetworkDevice> NetworkDevices { get; private set; }
		
		void OnStateChanged (object o, DeviceStateChangedArgs args)
		{
			Console.WriteLine ("A device state has changed: {0} new state: {1}", (o as NetworkDevice).ObjectPath, args.NewState);
		}
		
		public IEnumerable<string> ActiveConnections {
			get
			{
				foreach (ObjectPath conPath in (ObjectPath[]) BusObject.Get (BusName, "ActiveConnections"))
					yield return conPath.ToString ();
			}
		}
	}
}
