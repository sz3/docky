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
	enum OutputDevice {
		AUTO = 0
	}
	class NetworkMonitor
	{
		Dictionary<string, DeviceInfo> devices;
		
		public void printResults()
		{
			foreach (KeyValuePair<string, DeviceInfo> pair in this.devices)
			{
				Console.WriteLine(pair.Value.ToString());
			}
		}
		
		public void update()
		{
			using (StreamReader reader = new StreamReader ("/proc/net/dev"))
			{
					try 
					{
							string data = reader.ReadToEnd();
							char[] delimiters = new char[] { '\r', '\n' };
							//Console.WriteLine(data);
							foreach (string row in data.Split(delimiters)) {
								this.parseRow(row);
							}

					} 
					catch {
							// we dont care
					}
			}
		}
		
		public void parseRow(string row)
		{
			if(row.IndexOf(":") < 1) {
				return;
			}
			//Console.WriteLine(row);
			string devicename = row.Substring(0,row.IndexOf(':')).Trim();
			if(devicename == "lo") {
				return;
			}
			//Console.WriteLine(devicename);
			row = row.Substring(row.IndexOf(":"),row.Length-row.IndexOf(":"));
			//Console.WriteLine(row);
			Regex regex = new Regex("\\d+");
			//The row has the following format:
			//Inter-|   Receive												|  Transmit
			//face  |bytes	packets errs drop fifo frame compressed multicast|bytes	packets errs drop fifo colls carrier compressed
			//So we need fields 0(bytes-sent) and 8(bytes-received)
			MatchCollection collection = regex.Matches (row);
			//debug_collection(collection);
			DeviceInfo d;
			long rx, tx;
			double tx_rate, rx_rate;
			rx = Convert.ToInt64 (collection [0].Value);
			tx = Convert.ToInt64 (collection [8].Value);
			DateTime now = DateTime.Now;
			
			if (devices.ContainsKey(devicename)) {
				d = devices[devicename];
				TimeSpan diff = now - d.last_update;
				//todo: use now and d.last_update to calc the correct rate in bytes/seconds
				tx_rate = (tx - d.tx) / diff.TotalSeconds; //todo, adjust to seconds according to update_interval
				rx_rate = (rx - d.rx) / diff.TotalSeconds; //todo, adjust to seconds according to update_interval
				
			} else {
				d = new DeviceInfo();
				d.name = devicename;
				d.rx = rx;
				d.tx = tx;
				tx_rate = 0;
				rx_rate = 0;
				this.devices.Add(devicename,d);
			}
			d.last_update = now;
			d.tx_rate = tx_rate;
			d.rx_rate = rx_rate;
			d.tx = tx;
			d.rx = rx;
		}
		
		public DeviceInfo getDevice(OutputDevice n)
		{
			DeviceInfo d = null ;
			if(n == 0) {
				foreach (KeyValuePair<string, DeviceInfo> pair in this.devices) {
					if (d == null) {
						d = pair.Value;
						continue;
					}
					if ( (pair.Value.tx + pair.Value.rx) > (d.tx+d.rx)) {
						d = pair.Value;					
					}
				}
			}
			return d;
		}

		public NetworkMonitor()
		{
			devices = new Dictionary<string, DeviceInfo>();
		}
	}
}
