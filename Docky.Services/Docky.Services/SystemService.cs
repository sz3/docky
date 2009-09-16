//  
//  Copyright (C) 2009 Jason Smith
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
using System.Diagnostics;
using System.IO;

namespace Docky.Services
{


	public class SystemService
	{
		public event EventHandler ConnectionStatusChanged;
		public event EventHandler BatteryStateChanged;
		
		public bool NetworkConnected {
			get {
				return true;
			}
		}
		
		public bool OnBattery {
			get {
				return false;
			}
		}
		
		internal SystemService ()
		{
		}
		
		public void Email (string address)
		{
			Process.Start ("xdg-email", address);
		}
		
		public void Open (string uri)
		{
			Process.Start ("xdg-open", uri);
		}
		
		public void Execute (string executable)
		{
			if (File.Exists (executable)) {
				Process proc = new Process ();
				proc.StartInfo.FileName = executable;
				proc.StartInfo.UseShellExecute = false;
				proc.Start ();
			} else {
				Process.Start (executable);
			}
		}
		
		void OnConnectionStatusChanged ()
		{
			if (ConnectionStatusChanged != null)
				ConnectionStatusChanged (this, EventArgs.Empty);
		}
		
		void OnBatteryStateChanged ()
		{
			if (BatteryStateChanged != null)
				BatteryStateChanged (this, EventArgs.Empty);
		}
	}
}
