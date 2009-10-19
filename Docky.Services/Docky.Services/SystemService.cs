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
		
		public void Open (IEnumerable<string> uris)
		{
			uris.ToList ().ForEach (uri =>  Open (uri));
		}
		
		public void Open (GLib.File file)
		{
			Open (new [] { file });
		}
		
		public void Open (IEnumerable<GLib.File> files)
		{
			List<GLib.File> noMountNeeded = new List<GLib.File> ();
			
			// before we try to use the files, make sure they are mounted
			foreach (GLib.File f in files) {
				// if the path isn't empty, 
				// check if it's a local file or on VolumeMonitor's mount list.
				// if it is, skip it.
				if (!string.IsNullOrEmpty (f.Path)) {
					if (f.IsNative ||VolumeMonitor.Default.Mounts.Any (m => f.Path.Contains (m.Root.Path))) {
						noMountNeeded.Add (f);
						continue;
					}
				}
				f.MountEnclosingVolume (0, null, null, (o, args) => {
					// wait for the mount to finish
					try {
						if (f.MountEnclosingVolumeFinish (args))
							// FIXME: when we can get a dock item from the UID, redraw the icon here.
							Launch (new [] {f});
					// an exception can be thrown here if we are trying to mount an already mounted file
					// in that case, just launch it.
					} catch (GLib.GException) {
						Launch (new [] {f});
					}
				});
			}

			if (noMountNeeded.Any ())
				Launch (noMountNeeded);
		}

		void Launch (IEnumerable<GLib.File> files)
		{			
			AppInfo app = files.First ().QueryDefaultHandler (null);
			
			GLib.List launchList;
			
			if (app != null) {
				// check if the app supports files or Uris
				if (app.SupportsFiles) {
					launchList = new GLib.List (typeof (GLib.File));
					foreach (GLib.File f in files)
						launchList.Append (f);
					try {
						// if launching was successful, bail
						if (app.Launch (launchList, null))
							return;
					} catch (GLib.GException e) {
						Log<SystemService>.Error (e.Message);
						Log<SystemService>.Info (e.StackTrace);
					}
				} else if (app.SupportsUris) {
					launchList = new GLib.List (typeof (string));
					foreach (GLib.File f in files) {
						// FIXME: File.URI is crashing in the Sys.Uri constructor somewhere
						try {
							launchList.Append (f.Uri.ToString ());
						} catch { return; }
					}
					try {
						if (app.LaunchUris (launchList, null))
							return;
					} catch (GLib.GException e) {
						Log<SystemService>.Error (e.Message);
						Log<SystemService>.Info (e.StackTrace);
					}
				}
			}
			
			Log<SystemService>.Error ("Error opening files. The application doesn't support files/URIs or wasn't found.");
			// fall back on xdg-open
			Open (files.Select (f => f.Uri.ToString ()));
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
