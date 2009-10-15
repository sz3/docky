//  
//  Copyright (C) 2009 Jason Smith, Chris Szikszoy, Robert Dyer
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
		internal SystemService ()
		{
			InitializeBattery ();
			InitializeNetwork ();
		}
		
		public string SystemDataFolder {
			get {
				return Path.Combine (AssemblyInfo.DataDirectory, "docky");
			}
		}
		
		public string UserDataFolder {
			get {
				return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "docky");
			}
		}
		
		#region Network
		
		void InitializeNetwork ()
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
				Log<SystemService>.Error ("Could not initialize Network Manager dbus: '{0}'", e.Message);
				Log<SystemService>.Info (e.StackTrace);
				NetworkConnected = true;
			}
		}		
		
		public event EventHandler<ConnectionStatusChangeEventArgs> ConnectionStatusChanged;
		
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
			if (State == NetworkState.Connected)
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
		
		#region Battery
		
		public event EventHandler BatteryStateChanged;

		public bool OnBattery {
			get {
				return on_battery;
			}
		}
		
		void OnBatteryStateChanged ()
		{
			if (BatteryStateChanged != null)
				BatteryStateChanged (this, EventArgs.Empty);
		}
		
		const string PowerManagementName = "org.freedesktop.PowerManagement";
		const string PowerManagementPath = "/org/freedesktop/PowerManagement";
		const string DeviceKitPowerName = "org.freedesktop.DeviceKit.Power";
		const string DeviceKitPowerPath = "/org/freedesktop/DeviceKit/Power";
		
		delegate void BoolDelegate (bool val);
		
		[Interface(PowerManagementName)]
		interface IPowerManagement
		{
			bool GetOnBattery ();
			event BoolDelegate OnBatteryChanged;
		}
		
		[Interface(DeviceKitPowerName)]
		interface IDeviceKitPower : org.freedesktop.DBus.Properties
		{
			event Action OnChanged;
		}
		
		bool on_battery;
		
		IPowerManagement power;
		IDeviceKitPower devicekit;
		
		void InitializeBattery ()
		{
			// Set a sane default value for on_battery.  Thus, if we don't find a working power manager
			// we assume we're not on battery.
			on_battery = false;
			try {
				BusG.Init ();
				if (Bus.System.NameHasOwner (DeviceKitPowerName)) {
					devicekit = Bus.System.GetObject<IDeviceKitPower> (DeviceKitPowerName, new ObjectPath (DeviceKitPowerPath));
					devicekit.OnChanged += DeviceKitOnChanged;
					on_battery = (bool) devicekit.Get (DeviceKitPowerName, "on-battery");
					Log<SystemService>.Debug ("Using org.freedesktop.DeviceKit.Power for battery information");
				} else if (Bus.Session.NameHasOwner (PowerManagementName)) {
					power = Bus.Session.GetObject<IPowerManagement> (PowerManagementName, new ObjectPath (PowerManagementPath));
					power.OnBatteryChanged += PowerOnBatteryChanged;
					on_battery = power.GetOnBattery ();
					Log<SystemService>.Debug ("Using org.freedesktop.PowerManager for battery information");
				}
			} catch (Exception e) {
				Log<SystemService>.Error ("Could not initialize power manager dbus: '{0}'", e.Message);
				Log<SystemService>.Info (e.StackTrace);
			}
		}

		void PowerOnBatteryChanged (bool val)
		{
			on_battery = val;
			OnBatteryStateChanged ();
		}
		
		void DeviceKitOnChanged ()
		{
			bool newState = (bool) devicekit.Get (DeviceKitPowerName, "on-battery");
			
			if (on_battery != newState) {
				on_battery = newState;
				OnBatteryStateChanged ();
			}
		}
		
		#endregion
		
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
				if (executable.Contains (" ")) {
					string[] args = executable.Split (' ');
					
					Process.Start (args[0], executable.Substring (args[0].Length + 1));
				} else {
					Process.Start (executable);
				}
			}
		}
	}
}
