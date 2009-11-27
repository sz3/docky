//  
//  Copyright (C) 2009 Chris Szikszoy
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

using NDesk.DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	public class WirelessDevice : NetworkDevice
	{
		internal WirelessDevice (string objectPath) : base(objectPath)
		{
			this.WirelessProperties = new DBusObject<IWirelessDevice> ("org.freedesktop.NetworkManager", objectPath);
			this.WirelessProperties.BusObject.AccessPointAdded += OnAPAdded;
			this.WirelessProperties.BusObject.AccessPointRemoved += OnAPRemoved;
			
			VisibleAccessPoints = new Dictionary<string, List<WirelessAccessPoint>> ();
			foreach (ObjectPath APObjPath in WirelessProperties.BusObject.GetAccessPoints ()) {
				WirelessAccessPoint ap = new WirelessAccessPoint (APObjPath.ToString ());
				AddApToDict (ap);
			}
		}
		
		public DBusObject<IWirelessDevice> WirelessProperties { get; private set; }
		public Dictionary<string, List<WirelessAccessPoint>> VisibleAccessPoints { get; private set; }

		void OnAPAdded (string objectPath)
		{
			WirelessAccessPoint ap = new WirelessAccessPoint (objectPath);
			AddApToDict (ap);
			//DumpAPs ();
		}

		void OnAPRemoved (string objectPath)
		{
			RemoveFromDict (objectPath);
			//DumpAPs ();
		}
		
		void AddApToDict (WirelessAccessPoint ap)
		{
			lock (VisibleAccessPoints) {
				if (VisibleAccessPoints.ContainsKey (ap.SSID)) {
				    VisibleAccessPoints[ap.SSID].Add (ap);
					VisibleAccessPoints[ap.SSID].Sort ();
				} else {
					VisibleAccessPoints[ap.SSID] = new List<WirelessAccessPoint> (new [] {ap});
				}
			}
		}
		
		// FIXME -- fixed by adding lock (VisibleAccessPoints) ?
		void RemoveFromDict (string objPath)
		{
			lock (VisibleAccessPoints) {
				foreach (List<WirelessAccessPoint> ap in VisibleAccessPoints.Values)
					ap.RemoveAll (apt => apt.ObjectPath == objPath);
				
				// remove empty entries
				foreach (string key in VisibleAccessPoints.Keys.ToList ())
					if (VisibleAccessPoints[key].Count == 0)
						VisibleAccessPoints.Remove (key);
			}
		}
		
		public WirelessAccessPoint APBySSID (string ssid)
		{
			// Multiple APs per SSID are sorted by strength, this should always return the strongest AP
			if (VisibleAccessPoints.ContainsKey (ssid))
				return VisibleAccessPoints[ssid].First ();
			return null;
		}

		public WirelessAccessPoint ActiveAccessPoint {
			get {
				string access = WirelessProperties.BusObject.Get (WirelessProperties.BusName, "ActiveAccessPoint").ToString ();
				foreach (string key in VisibleAccessPoints.Keys)
					if (VisibleAccessPoints[key].Where (ap => ap.ObjectPath == access).Count () > 0)
						return VisibleAccessPoints[key].Where (ap => ap.ObjectPath == access).First ();
				
				// this should also catch the case of no active AP, where access = "/";
				return null;
			}
		}
		
		public string HWAddress {
			get { return WirelessProperties.BusObject.Get (WirelessProperties.BusName, "HwAddress").ToString (); }
		}
	}
}
