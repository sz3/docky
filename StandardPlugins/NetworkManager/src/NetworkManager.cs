
using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	
	public class NetworkManager
	{
		
		public delegate void DeviceStateChangedHandler (object sender, DeviceStateChangedArgs args);
		
		public event DeviceStateChangedHandler DeviceStateChanged;
		
		public NetworkManager ()
		{
			ConManager = new ConnectionManager ();
			DevManager = new DeviceManager ();
			
			DevManager.NetworkDevices.ForEach (dev => dev.StateChanged += OnDevStateChanged);
		}

		void OnDevStateChanged(object o, DeviceStateChangedArgs args)
		{
			NetworkDevice dev = o as NetworkDevice;
			if (args.NewState == DeviceState.Active) {
				Console.WriteLine ("A device has been activated! {0}", dev.ObjectPath);
				Console.WriteLine ("{0}, {1}, {2}", dev.IP4Address.ToString (), dev.Gateway.ToString (), dev.PrimaryDNS.ToString ());				
			}
			Console.WriteLine ("Active Connections: {0}", ActiveConnections.Count ());
			if (DeviceStateChanged != null)
				DeviceStateChanged (dev, args);
		}
		
		public ConnectionManager ConManager { get; private set; }
		public DeviceManager DevManager { get; private set; }
		
		public IEnumerable<NetworkConnection> ActiveConnections {
			get
			{
				foreach (string active in DevManager.ActiveConnections) {
					DBusObject<IActiveConnection> ActiveConnection = new DBusObject<IActiveConnection> ("org.freedesktop.NetworkManager", active);
					if (ActiveConnection.BusObject.Get (ActiveConnection.BusName, "ServiceName").ToString ().Contains ("System"))
						yield return ConManager.SystemConnections.Where (con => con.ObjectPath == ActiveConnection.BusObject.Get (ActiveConnection.BusName, "Connection").ToString ()).First ();
					else
						yield return ConManager.UserConnections.Where (con => con.ObjectPath == ActiveConnection.BusObject.Get (ActiveConnection.BusName, "Connection").ToString ()).First ();
				}
			}
		}

		public void ConnectTo (WirelessAccessPoint ap)
		{
			NetworkConnection connection;
			
			try {
				connection = ConManager.AllConnections.OfType<WirelessConnection> ().Where (con => (con as WirelessConnection).SSID == ap.SSID).First ();
				ConnectTo (connection);
			}
			catch {
				//We're trying to connect to an AP but no connection entry exists.
				//If we can figure out how to manually create a connection behind the scenes, we can remove this.
				Docky.Services.DockServices.System.RunOnThread ( () => {
					Process.Start ("nm-connection-editor --type=802-11-wireless");
				});
			}
		}
		
		public void ConnectTo (NetworkConnection con)
		{
			Console.WriteLine ("Connecting to {0}", con.ConnectionName);
			
			NetworkDevice dev; 
			string specObj;
			if (con is WirelessConnection) {
				dev = DevManager.NetworkDevices.OfType<WirelessDevice> ().First ();
				specObj = (dev as WirelessDevice).APBySSID ((con as WirelessConnection).SSID).ObjectPath;
			} else if (con is WiredConnection) {
				dev = DevManager.NetworkDevices.OfType<WiredDevice> ().First ();
				specObj = "/";
			} else {
				return;
			}
			
			string serviceName;
			if (con.Owner == ConnectionOwner.System)
				serviceName = "org.freedesktop.NetworkManagerSystemSettings";
			else
				serviceName = "org.freedesktop.NetworkManagerUserSettings";
			string conStr = con.ObjectPath;
			
			Console.WriteLine ("{0}\n{1}\n{2}\n{3}", serviceName, conStr, dev.ObjectPath, specObj);
			
			DevManager.BusObject.ActivateConnection(serviceName, new ObjectPath (conStr), new ObjectPath (dev.ObjectPath), new ObjectPath (specObj));
		}
	}
}