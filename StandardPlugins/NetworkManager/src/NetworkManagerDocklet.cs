//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer, Jason Smith
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

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
			
			GLib.Timeout.Add (10 * 1000, delegate {
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
			//Console.WriteLine (args.NewState);
			ReDraw ();
		}
		
		public NetworkManager NM { get; private set; }
		
		void ReDraw ()
		{
			Gtk.Application.Invoke (delegate {
				Icon = SetDockletIcon ();
				QueueRedraw ();
			});
		}
		
		int iconStage;
		int iconStep;
		uint iconTimer;
		
		string AnimatedIcon (NetworkDevice dev)
		{
			if (iconTimer == 0)
				iconTimer = GLib.Timeout.Add (100, delegate {
					ReDraw ();
					return true;
				});
			
			switch (dev.State) {
			case DeviceState.Configuring:
				iconStage = 1;
				break;
			case DeviceState.IPConfiguring:
				iconStage = 2;
				break;
			default:
			case DeviceState.Preparing:
				iconStage = 3;
				break;
			}
			
			if (iconStep == 11)
				iconStep = 0;
			return string.Format ("nm-stage0{0}-connecting{1:00}", iconStage, ++iconStep);
		}
		
		string SetDockletIcon ()
		{
			try {
				// currently connecting (animated)
				NetworkDevice dev = NM.DevManager.NetworkDevices
					.Where (d => d.State == DeviceState.Configuring || d.State == DeviceState.IPConfiguring || d.State == DeviceState.Preparing)
					.FirstOrDefault ();
				if (dev != null)
					return AnimatedIcon (dev);
				
				if (iconTimer != 0) {
					GLib.Source.Remove (iconTimer);
					iconStep = 0;
					iconStage = 0;
				}
				
				// no connection
				if (!NM.ActiveConnections.Any ())
					return "nm-no-connection";
				
				// wireless connection
				if (NM.ActiveConnections.OfType<WirelessConnection> ().Any ()) {
					string ssid = NM.ActiveConnections.OfType<WirelessConnection> ().First ().SSID;
					byte strength = NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().First ().APBySSID (ssid).Strength;
					return APIconFromStrength (strength);
				}
			} catch {
				// FIXME why do we default to this?
				return APIconFromStrength ((byte) 100);
			}
			
			// default to wired connection
			return "nm-device-wired";
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

		public override MenuList GetMenuItems ()
		{
			MenuList list = base.GetMenuItems ();
			
			List<MenuItem> wifi = list[MenuListContainer.Actions];
			
			int count = 0;
			if (NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().Any ()) {
				foreach (WirelessDevice device in NM.DevManager.NetworkDevices.OfType<WirelessDevice> ()) {
					foreach (List<WirelessAccessPoint> val in device.VisibleAccessPoints.Values.OrderByDescending (ap => ap.Max (wap => wap.Strength))) {
						if (count > 7)
							break;
						
						wifi.Add (MakeConEntry (val.OrderByDescending (wap => wap.Strength).First ()));
						count++;
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
		
		MenuItem MakeConEntry (WirelessAccessPoint ap)
		{
			string apName = ap.SSID;
			string icon = APIconFromStrength (ap.Strength);
			bool bold = NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().Any (dev => dev.ActiveAccessPoint.SSID == ap.SSID);
			
			Docky.Menus.MenuItem item = new Docky.Menus.MenuItem (apName, icon, (o, a) => NM.ConnectTo (ap));
			item.Bold = bold;
			
			return item;
		}
	}
}
