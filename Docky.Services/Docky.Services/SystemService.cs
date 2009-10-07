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

using NDesk.DBus;
using org.freedesktop.DBus;

namespace Docky.Services
{

	public class SystemService
	{
		public event EventHandler<ConnectionStatusChangeEventArgs> ConnectionStatusChanged;
		public event EventHandler BatteryStateChanged;

		internal SystemService ()
		{
			try {
				BusG.Init ();
				if (Bus.System.NameHasOwner (NetworkManagerName)) {
					network = Bus.System.GetObject<INetworkManager> (NetworkManagerName, new ObjectPath (NetworkManagerPath));
					network.StateChanged += OnConnectionStatusChanged;
					SetConnected ();
				}
			} catch (Exception e) {
				// if something bad happened, log the error and assume we are connected
				// FIXME: use proper logging
				//Log<NetworkService>.Error ("Could not initialize Network Manager dbus: {0}", e.Message);
				//Log<NetworkService>.Debug (e.StackTrace);
				NetworkConnected = true;
			}
		}		
		
		#region Network
		
		const string NetworkManagerName = "org.freedesktop.NetworkManager";
		const string NetworkManagerPath = "/org/freedesktop/NetworkManager";
		INetworkManager network;
		
		[Interface(NetworkManagerName)]
		interface INetworkManager : org.freedesktop.DBus.Properties
		{
			event StateChangedHandler StateChanged;
		}
		
		delegate void StateChangedHandler (uint state);
		
		public bool NetworkConnected { get; private set; }
		
		void OnConnectionStatusChanged (uint state)
		{
			NetworkState newState = (NetworkState) Enum.ToObject (typeof (NetworkState), state);
			SetConnected ();
			
			if (ConnectionStatusChanged != null)
				ConnectionStatusChanged (this, new ConnectionStatusChangeEventArgs (newState));
		}
		
		void SetConnected ()
		{
			if (this.State == NetworkState.Connected)
				NetworkConnected = true;
			else
				NetworkConnected = false;
		}
		
		NetworkState State {
			get	{ 
				try {
					return (NetworkState) Enum.ToObject (typeof (NetworkState), network.Get (NetworkManagerName, "State"));
				} catch (Exception) {
					return NetworkState.Unknown;
				}
			}
		}		
		
		#endregion
		
		public bool OnBattery {
			get {
				return false;
			}
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
		
		void OnBatteryStateChanged ()
		{
			if (BatteryStateChanged != null)
				BatteryStateChanged (this, EventArgs.Empty);
		}
	}
}
