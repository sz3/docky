//  
//  Copyright (C) 2011 Florian Dorn
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using Gdk;
using GLib;

namespace NetworkMonitorDocklet
{
	enum OutputDevice
	{
		AUTO = 0
	}
	
	class NetworkMonitor
	{
		Dictionary<string, DeviceInfo> devices = new Dictionary<string, DeviceInfo> ();
		
		public NetworkMonitor ()
		{
		}

		public void printResults ()
		{
			foreach (DeviceInfo device in devices.Values)
				Console.WriteLine (device.ToString ());
		}
		
		public void update ()
		{
			try  {
				using (StreamReader reader = new StreamReader ("/proc/net/dev")) {
					string data = reader.ReadToEnd ();
					char[] delimiters = new char[] { '\n' };
					foreach (string row in data.Split (delimiters))
						parseDeviceInfoString (row);
				}
			} catch {
				// we dont care
			}
		}
		
		public void parseDeviceInfoString (string row)
		{
			if (row.IndexOf (":") < 1)
				return;
			
			Regex regex = new Regex (@"\w+|\d+");
			MatchCollection collection = regex.Matches (row);

			//The row has the following format:
			//Inter-|   Receive                                                |  Transmit
			//face |bytes    packets errs drop fifo frame compressed multicast|bytes    packets errs drop fifo colls carrier compressed
			//So we need fields 0 device name 1 (bytes-sent) and 8 (bytes-received)
			string devicename = collection [0].Value;
			long rx = Convert.ToInt64 (collection [1].Value);
			long tx = Convert.ToInt64 (collection [9].Value);
			
			DateTime now = DateTime.Now;
			DeviceInfo d;
			if (devices.TryGetValue (devicename, out d)) {
				d.txRate = (tx - d.tx) / (now - d.lastUpdated).TotalSeconds;
				d.rxRate = (rx - d.rx) / (now - d.lastUpdated).TotalSeconds;
			} else {
				d = new DeviceInfo (devicename);
				devices.Add (devicename, d);
			}
			
			d.lastUpdated = now;
			d.tx = tx;
			d.rx = rx;
		}
		
		public DeviceInfo getDevice (OutputDevice n)
		{
			if (n != OutputDevice.AUTO)
				return null;
			
			DeviceInfo d = null;
			foreach (DeviceInfo device in devices.Values)
				if (d == null || device.sumRate > d.sumRate)
					d = device;
			
			return d;
		}

	}
}
