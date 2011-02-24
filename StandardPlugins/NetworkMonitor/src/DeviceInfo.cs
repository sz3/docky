//  
//  Copyright (C) 2011 Florian Dorn, Rico Tzschichholz
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
	class DeviceInfo 
	{
		public string name;
		public long uploadedBytes = 0;
		public long downloadedBytes = 0;
		public double uploadRate = 0.0;
		public double downloadRate = 0.0;
		public DateTime lastUpdated;

		public double sumRate {
			get {
				return uploadRate + downloadRate;
			}
		}
		
		public DeviceInfo (string _name) : this (_name, 0, 0)
		{
		}

		public DeviceInfo (string _name, long _downloadedBytes, long _uploadedBytes)
		{
			name = _name;
			
			lastUpdated = DateTime.Now;

			downloadedBytes = _downloadedBytes;
			uploadedBytes = _uploadedBytes;
		}

		public void Update (long new_downloadedBytes, long new_uploadedBytes)
		{
			var now = DateTime.Now;
			
			uploadRate = (new_uploadedBytes - uploadedBytes) / (now - lastUpdated).TotalSeconds;
			downloadRate = (new_downloadedBytes - downloadedBytes) / (now - lastUpdated).TotalSeconds;
			
			uploadedBytes = new_uploadedBytes;
			downloadedBytes = new_downloadedBytes;

			lastUpdated = now;
		}
		
		public override string ToString ()
		{
			return string.Format ("{0}: {2,10} down {1,10} up (Total: {3}/{4})",
			                      name,
			                      BytesToFormattedString (uploadRate),
			                      BytesToFormattedString (downloadRate),
			                      BytesToFormattedString (uploadedBytes),
			                      BytesToFormattedString (downloadedBytes));
		}
		
		public string FormatUpDown (bool up)
		{
			double rate = downloadRate;
			
			if (rate < 1)
				return "-";
			
			if (up)
				rate = uploadRate;
			
			return BytesToFormattedString (rate, true);
		}
		
		static string BytesToFormattedString (double bytes)
		{
			return BytesToFormattedString (bytes, false);
		}
		
		static string BytesToFormattedString (double bytes, bool per_sec)
		{
			int kilo = 1024;
			string format, unit;
			
			if (bytes < kilo) {
				format = "{0:0} {1}";
				unit = "B";
			} else if (bytes < (kilo * kilo)) {
				if (bytes < (100 * kilo)) {
					format = "{0:0.0} {1}";
				} else {
					format = "{0:0} {1}";
				}
				bytes /= kilo;
				unit = "K";
			} else {
				format = "{0:0.0} {1}";
				bytes /= (kilo * kilo);
				unit = "M";
			}
			
			if (per_sec)
				unit = string.Format ("{0}/s", unit);
			
			return string.Format (format, bytes, unit);
		}
	}
}
