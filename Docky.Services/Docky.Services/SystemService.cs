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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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
			NetworkConnected = true;
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
			event Action Changed;
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
					devicekit.Changed += HandleDeviceKitChanged;
					HandleDeviceKitChanged ();
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
			if (on_battery != val) {
				on_battery = val;
				OnBatteryStateChanged ();
			}
		}
		
		void HandleDeviceKitChanged ()
		{
			bool newState = (bool) devicekit.Get (DeviceKitPowerName, "OnBattery");
			
			if (on_battery != newState) {
				on_battery = newState;
				OnBatteryStateChanged ();
			}
		}
		
		#endregion
		
		public void Email (string address)
		{
			Execute ("xdg-email " + address);
		}
		
		public void Open (string uri)
		{
			Execute ("xdg-open " + uri);
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
				// try to mount, if successful launch, otherwise (it's possibly already mounted) try to launch anyways
				f.MountWithActionAndFallback (() => Launch (new [] {f}), () => Launch (new [] {f}));
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
						// try to use GLib.File.Uri first, if that throws an exception,
						// catch and use P/Invoke to libdocky.  If that's still null, warn & skip the file.
						try {
							launchList.Append (f.Uri.ToString ());
						} catch (UriFormatException) { 
							string uri = f.StringUri ();
							if (string.IsNullOrEmpty (uri)) {
								Log<SystemService>.Warn ("Failed to retrieve URI for {0}.  It will be skipped.", f.ParsedName);
								continue;
							}
							launchList.Append (uri);
						}
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
			Open (files.Select (f => f.StringUri ()));
		}

		
		public void Execute (string executable)
		{
			if (System.IO.File.Exists (executable)) {
				System.Diagnostics.Process proc = new System.Diagnostics.Process ();
				proc.StartInfo.FileName = executable;
				proc.StartInfo.UseShellExecute = false;
				proc.Start ();
			} else {
				System.Diagnostics.Process proc;
				if (executable.Contains (" ")) {
					string[] args = executable.Split (' ');
					
					Log<SystemService>.Debug ("Calling: " + args[0] + " \"" + executable.Substring (args[0].Length + 1) + "\"");
					proc = System.Diagnostics.Process.Start (args[0], "\"" + executable.Substring (args[0].Length + 1) + "\"");
				} else {
					Log<SystemService>.Debug ("Calling: " + executable);
					proc = System.Diagnostics.Process.Start (executable);
				}
				proc.Dispose ();
			}
		}
		
		public void RunOnThread (Action action, TimeSpan delay)
		{
			RunOnThread (() => {
				System.Threading.Thread.Sleep (delay);
				action ();
			});
		}
		
		public void RunOnThread (Action action, int delay)
		{
			RunOnThread (action, new TimeSpan (0, 0, 0, 0, delay));
		}
		
		public void RunOnThread (Action action)
		{
			System.Threading.Thread newThread = new System.Threading.Thread (() =>
			{
				try {
					action ();
				} catch (ThreadAbortException) {
				} catch (Exception e) {
					Log<SystemService>.Error ("Error in RunOnThread: {0}", e.Message);
					Log<SystemService>.Debug (e.StackTrace);
				}
			});
			
			newThread.IsBackground = true;
			newThread.Start ();
		}
		
		public void RunOnMainThread (Action action)
		{
			Gtk.Application.Invoke ((sender, arg) => {
				try {
					action ();
				} catch (Exception e) {
					Log<SystemService>.Error ("Error in RunOnMainThread: {0}", e.Message);
					Log<SystemService>.Debug (e.StackTrace);
				}
			});
		}
		
		public void RunOnMainThread (Action action, int delay)
		{
			RunOnMainThread (action, new TimeSpan (0, 0, 0, 0, delay));
		}
		
		public void RunOnMainThread (Action action, TimeSpan delay)
		{
			RunOnThread (() => RunOnMainThread (action), delay);
		}
		
		public void SetProcessName (string name)
		{
			NativeInterop.prctl (15 /* PR_SET_NAME */, name);
		}
	}
}
