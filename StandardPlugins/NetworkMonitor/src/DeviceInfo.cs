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
		public string name;
		public long tx;
		public long rx;
		public double txRate;
		public double rxRate;
		public DateTime lastUpdated;

		override public string ToString ()
		{
			return string.Format("{0}: {2,10} down {1,10} up (Total: {3}/{4})",
							this.name, 
							bytes_to_string(this.txRate), 
							bytes_to_string(this.rxRate), 
							bytes_to_string(this.tx,false), 
							bytes_to_string(this.rx,false));
		}
		
		public string formatUpDown (bool up)
		{
			double rate = rxRate;
			if (rate < 1)
				return "-";
			if (up)
				rate = txRate;
			return this.bytes_to_string (rate,true);
		}
		public string bytes_to_string (double bytes)
		{
			return bytes_to_string (bytes,false);
		}
		public string bytes_to_string (double bytes,bool per_sec)
		{
			int kilo = 1024;
			string format,unit;
			if(bytes < kilo)
			{
				format = "{0:0} {1}";
				unit = ("B");
			} 
			else if (bytes < (kilo * kilo)) 
			{
				if(bytes < (100*kilo)) {
					format = "{0:0.0} {1}";
				} else {
					format = "%.0f %s";
					format = "{0:0} {1}";
				}
				bytes /= kilo;
				unit = ("K");
			} else {
				format = "{0:0.0} {1}";
				bytes /= (kilo * kilo);
				unit = ("M");
				
			}
			if(per_sec)
				unit = string.Format ("{0}/s",unit);
			return string.Format (format,bytes,unit);
		}

	}
}
