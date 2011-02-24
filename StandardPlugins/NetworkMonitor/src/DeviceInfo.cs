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

namespace NetworkMonitorDocklet 
{
	class DeviceInfo {
		public String name;
		public String ip = "xxx.xxx.xxx.xxx";
		public long tx;
		public long rx;
		public double tx_rate;
		public double rx_rate;
		public long bytesIn {get; set; }
		public long bytesOut {get; set; }
		public DateTime last_update;

		override public string ToString() {
			return string.Format("{0}: {2,10} down {1,10} up (Total: {3}/{4})", this.name, bytes_to_string(this.tx_rate), bytes_to_string(this.rx_rate), bytes_to_string(this.tx,false),bytes_to_string(this.rx,false));
		}
		
		public String formatUpDown(bool up)
		{
			double rate = rx_rate;
			if (up) {
				rate = tx_rate;
			}
			if (rate < 1) {
				return "-";
			}			 
			return this.bytes_to_string(rate,true);
		}
		public String bytes_to_string(double bytes)
		{
			return bytes_to_string(bytes,false);
		}
		public String bytes_to_string(double bytes,bool per_sec)
		{
			int kilo = 1024;
			String format,unit;
			if(bytes < kilo)
			{
				format = "{0:0} {1}";
				if(per_sec) {
					unit = ("B/s");
				} else {
					unit = ("B");
				}
			} 
			else if (bytes < (kilo * kilo)) 
			{
			//kilo
				if(bytes < (100*kilo)) {
					format = "{0:0.0} {1}";
				} else {
					format = "%.0f %s";
					format = "{0:0} {1}";
				}
				bytes /= kilo;
				if (per_sec) {
						unit = ("K/s");
				} else {
						unit = ("KB");
				}
			} else {
				format = "{0:0.0} {1}";
				bytes /= (kilo * kilo);

				if (per_sec) {
						unit = ("M/s");
				} else {
						unit = ("M");
				}
			}
			return String.Format(format,bytes,unit);
		}

	}
}
