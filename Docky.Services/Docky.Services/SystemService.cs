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
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

using GLib;

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
			System.Diagnostics.Process.Start ("xdg-email", address);
		}
		
		public void Open (string uri)
		{
			System.Diagnostics.Process.Start ("xdg-open", uri);
		}
		
		public void Open (IEnumerable<GLib.File> files)
		{			
			int nMounts = 0;
			
			// before we try to use the files, make sure they are mounted
			foreach (GLib.File f in files) {
				// it doesn't need to be mounted
				if (f.IsNative)
					continue;
				// it's already mounted
				if (VolumeMonitor.Default.Mounts.Any (m => m.Root.Uri == f.Uri))
				    continue;
				nMounts++;
				f.MountEnclosingVolume (0, null, null, (o, args) => {
					while (!f.MountEnclosingVolumeFinish (args));
					nMounts--;
					MaybeLaunch (files, nMounts);
				});
			}

			MaybeLaunch (files, nMounts);
		}

		void MaybeLaunch (IEnumerable<GLib.File> files, int nMounts)
		{
			if (nMounts > 0)
				return;
			
			AppInfo app = files.First ().QueryDefaultHandler (null);
			
			GLib.List launchList;
			
			if (app != null) {
				// check if the app supports files or Uris
				if (app.SupportsFiles) {
					launchList = new GLib.List (typeof (GLib.File));
					foreach (GLib.File f in files)
						launchList.Append (f);
					app.Launch (launchList, null);
					return;
				} else if (app.SupportsUris) {
					launchList = new GLib.List (typeof (string));
					foreach (GLib.File f in files)
						launchList.Append (f.Uri.ToString ());
					app.LaunchUris (launchList, null);
					return;
				}
			}
			
			Log<SystemService>.Error ("Error opening files. The application doesn't support files/URIs or wasn't found.");
			// fall back on xdg-open
			files.ToList ().ForEach (f =>  Open (f.Uri.ToString ()));
		}

		
		public void Execute (string executable)
		{
			if (System.IO.File.Exists (executable)) {
				System.Diagnostics.Process proc = new System.Diagnostics.Process ();
				proc.StartInfo.FileName = executable;
				proc.StartInfo.UseShellExecute = false;
				proc.Start ();
			} else {
				if (executable.Contains (" ")) {
					string[] args = executable.Split (' ');
					
					System.Diagnostics.Process.Start (args[0], executable.Substring (args[0].Length + 1));
				} else {
					System.Diagnostics.Process.Start (executable);
				}
			}
		}
	}
}
