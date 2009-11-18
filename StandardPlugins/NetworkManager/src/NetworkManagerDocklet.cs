
using System;
using System.Linq;
using System.Collections.Generic;

using Gdk;
using Cairo;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace NetworkManagerDocklet
{

	public class NetworkManagerDocklet : IconDockItem
	{
		
		public NetworkManagerDocklet ()
		{
			NM = new NetworkManager ();
			NM.DeviceStateChanged += OnDeviceStateChanged;
			
			HoverText = "Network Manager";
			Icon = SetDockletIcon ();
			
			GLib.Timeout.Add (30 * 1000, delegate {
				ReDraw ();
				return true;
			});
		}
		
		public override string UniqueID ()
		{
			return GetType ().FullName;
		}
		
		void OnDeviceStateChanged (object sender, DeviceStateChangedArgs args)
		{
			ReDraw ();
		}
		
		public NetworkManager NM { get; private set; }
		
		void ReDraw ()
		{
			Gtk.Application.Invoke (delegate {
				QueueRedraw ();
			});
		}
		
		string SetDockletIcon ()
		{
			string icon = "nm-device-wired";
			if (NM.ActiveConnections.Any ()) {
				if (NM.ActiveConnections.OfType<WirelessConnection> ().Any ()) {
					string ssid = NM.ActiveConnections.OfType<WirelessConnection> ().First ().SSID;
					byte strength = NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().First ().APBySSID (ssid).Strength;
					icon = APIconFromStrength (strength);
				}
				if (NM.DevManager.NetworkDevices.Any (dev => dev.State == DeviceState.Configuring ||
				    dev.State == DeviceState.IPConfiguring || dev.State == DeviceState.Preparing))
					icon = "nm-stage01-connecting01";
			} else {
				icon = "nm-no-connection";
			}
			return icon;
		}
		
		string APIconFromStrength (byte strength)
		{
			string icon = "gnome-netstatus-{0}";
			if (strength >= 75)
				icon = string.Format (icon, "75-100");
			else if (strength >= 50)
				icon = string.Format (icon, "50-74");
			else if (strength >= 25)
				icon = string.Format (icon, "25-49");
			else
				icon = string.Format (icon, "0-24");
			return icon;
		}

		#region IRightClickable implementation 
		
		public event EventHandler RemoveClicked;
		
		public override MenuList GetMenuItems ()
		{
			MenuList list = base.GetMenuItems ();
			
			List<MenuItem> wifi = list[MenuListContainer.Actions];
			
			if (NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().Any ()) {
				foreach (WirelessDevice device in NM.DevManager.NetworkDevices.OfType<WirelessDevice> ()) {
					foreach (KeyValuePair<string, List<WirelessAccessPoint>> kvp in device.VisibleAccessPoints) {
						wifi.Add (MakeConEntry (kvp.Value.First ()));
					}
				}
			}
			
			return list;
		}
		
//		public IEnumerable<AbstractMenuArgs> GetMenuItems ()
//		{
//			List<AbstractMenuArgs> cons = new List<AbstractMenuArgs> ();
//
//			//show Wired networks (if carrier is true)
//			if (NM.DevManager.NetworkDevices.OfType<WiredDevice> ().Any (dev => (dev as WiredDevice).Carrier == true)) {
//				NM.ConManager.AllConnections.OfType<WiredConnection> ().ForEach<WiredConnection> ( con => {
//					cons.Add (MakeConEntry (con));
//				});
//			}
//
//			//show wireless connections if wireless is available
//			if (NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().Any ()) {
//				cons.Add (new SeparatorMenuButtonArgs ());
//				NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().ForEach<WirelessDevice> (dev => {
//					dev.VisibleAccessPoints.Values.ForEach <List<WirelessAccessPoint>> ( apList => {
//						cons.Add (MakeConEntry (apList.First ()));
//					});
//				});
//			}
//			
//			foreach (AbstractMenuArgs arg in cons)
//			{
//				yield return arg;
//			}
//			
//			//yield return new WidgetMenuArgs (box);
//			
//			//yield return new SimpleMenuButtonArgs (() => Console.WriteLine ("asdf"),"Click me!", "network-manager");
//		}
//		
//		AbstractMenuArgs MakeConEntry (NetworkConnection con)
//		{
//			string name = con.ConnectionName;
//			string icon = "nm-device-wired";
//			if (NM.ActiveConnections.Contains (con))
//				name = string.Format ("<b>{0}</b>",name);
//			
//			return new SimpleMenuButtonArgs (() => NM.ConnectTo (con), name, icon);
//		}
		
		MenuItem MakeConEntry (WirelessAccessPoint ap)
		{
			string apName;
			string icon = APIconFromStrength (ap.Strength);
			
			if (NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().Any (dev => dev.ActiveAccessPoint == ap))
			    apName = string.Format ("<b>{0}</b>", ap.SSID);
			else
				apName = ap.SSID;
			
			return new Docky.Menus.MenuItem (apName, icon, (o, a) => NM.ConnectTo (ap));
		}
		
		#endregion 
	}
}
