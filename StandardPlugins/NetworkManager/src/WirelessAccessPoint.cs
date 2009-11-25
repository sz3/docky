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

using Docky.Services;

namespace NetworkManagerDocklet
{
	public class WirelessAccessPoint : DBusObject<IAccessPoint>, IComparable<WirelessAccessPoint>
	{
		public WirelessAccessPoint(string objectPath) : base ("org.freedesktop.NetworkManager", objectPath)
		{
		}
		
		public string SSID {
			get {
				try {
					return System.Text.ASCIIEncoding.ASCII.GetString ((byte[]) BusObject.Get (BusName, "Ssid"));
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (ObjectPath);
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return "Unknown SSID";
				}
			}
		}

		public byte Strength {
			get {
				try {
					return (byte) BusObject.Get (BusName, "Strength");
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return (byte) 0;
				}
			}
		}
		
		#region IComparable<WirelessAccessPoint>
		
		public int CompareTo (WirelessAccessPoint other)
		{
			if (this.Strength >= other.Strength)
				return -1;
			return 1;
		}
		
		#endregion
	}
}
