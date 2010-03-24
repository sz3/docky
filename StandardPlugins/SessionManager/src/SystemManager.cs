//  
//  Copyright (C) 2010 Rico Tzschichholz
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

using NDesk.DBus;
using org.freedesktop.DBus;

using Mono.Unix;
using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace SessionManager
{

	public class SystemManager
	{

		const string SessionBusName = "org.freedesktop.DBus";
		const string SessionBusPath = "/org/freedesktop/DBus";
		const string SystemBusName = "org.freedesktop.DBus";
		const string SystemBusPath = "/org/freedesktop/DBus";

		const string UPowerName = "org.freedesktop.UPower";
		const string UPowerPath = "/org/freedesktop/UPower";
		const string UPowerIface = "org.freedesktop.UPower";

		const string DeviceKitPowerName = "org.freedesktop.DeviceKit.Power";
		const string DeviceKitPowerPath = "/org/freedesktop/DeviceKit/Power";
		const string DeviceKitPowerIface = "org.freedesktop.DeviceKit.Power";

		const string ConsoleKitName = "org.freedesktop.ConsoleKit";
		const string ConsoleKitPath = "/org/freedesktop/ConsoleKit/Manager";
		const string ConsoleKitIface = "org.freedesktop.ConsoleKit.Manager";

		IBus SessionBus;
		IBus SystemBus;

		FileMonitor reboot_required_monitor;
		
		public event EventHandler BusChanged;
		public event EventHandler CapabilitiesChanged;
		public event EventHandler RebootRequired;
		
		[Interface (DeviceKitPowerIface)]
		interface IDeviceKitPower : org.freedesktop.DBus.Properties
		{
			void Hibernate ();
			void Suspend ();

			//bool CanHibernate { get; }
			//bool CanSuspend { get; }
			
			event Action Changed;
		}
		
		[Interface (UPowerIface)]
		interface IUPower : org.freedesktop.DBus.Properties
		{
			bool HibernateAllowed ();
			bool SuspendAllowed ();
			void Hibernate ();
			void Suspend ();

			//bool CanHibernate { get; }
			//bool CanSuspend { get; } 
			
			event Action Changed;
		}

		[Interface (ConsoleKitIface)]
		interface IConsoleKit
		{
			bool CanStop ();
			bool CanRestart ();

			void Stop ();
			void Restart ();
		}

		bool GetBoolean (org.freedesktop.DBus.Properties dbusobj, string path, string propname) 
		{
			return Boolean.Parse (dbusobj.Get (path, propname).ToString ());
		}
		
		IDeviceKitPower devicekit;
		IUPower upower;
		IConsoleKit consolekit;
		
		
		private static SystemManager instance;

		public static SystemManager GetInstance ()
		{
			if (instance == null)
				instance = new SystemManager ();
			return instance;
		}

		private SystemManager ()
		{
			//BusG.Init ();
			
			try {
				//SessionBus = Bus.Session.GetObject<IBus> (SessionBusName, new ObjectPath (SessionBusPath));
				SystemBus = Bus.System.GetObject<IBus> (SystemBusName, new ObjectPath (SystemBusPath));
				
				SystemBus.NameOwnerChanged += delegate(string name, string old_owner, string new_owner) {
					
					if (name != UPowerName && name != DeviceKitPowerName && name != ConsoleKitName)
						return;

					Log<SystemManager>.Debug ("DBus services changed, reconnecting now");
					
					if (upower != null)
						upower = null;
					
					if (devicekit != null)
						devicekit = null;

					if (consolekit != null)
						consolekit = null;
					
					Initialize ();
					HandlePowerBusChanged ();
					HandleCapabilitiesChanged ();
				};
				
				Initialize ();
				
				// Set up file monitor to watch for reboot_required file
				GLib.File reboot_required_file = FileFactory.NewForPath ("/var/run/reboot-required");
				reboot_required_monitor = reboot_required_file.Monitor (FileMonitorFlags.None, null);
				reboot_required_monitor.RateLimit = 10000;
				reboot_required_monitor.Changed += HandleRebootRequired;

			} catch (Exception e) {
				Log<SessionManagerItem>.Error (e.Message);
			}
			
		}
		
		void Initialize ()
		{
			try {
				
				if (upower == null && Bus.System.NameHasOwner (UPowerName)) {
					upower = Bus.System.GetObject<IUPower> (UPowerName, new ObjectPath (UPowerPath));
					upower.Changed += HandleCapabilitiesChanged;
					Log<SystemManager>.Debug ("Using UPower dbus service");
				} else if (devicekit == null && Bus.System.NameHasOwner (DeviceKitPowerName)) {
					devicekit = Bus.System.GetObject<IDeviceKitPower> (DeviceKitPowerName, new ObjectPath (DeviceKitPowerPath));
					devicekit.Changed += HandleCapabilitiesChanged;
					Log<SystemManager>.Debug ("Using DeviceKit.Power dbus service");
				}
				
				if (consolekit == null && Bus.System.NameHasOwner (ConsoleKitName)) {
					consolekit = Bus.System.GetObject<IConsoleKit> (ConsoleKitName, new ObjectPath (ConsoleKitPath));
					Log<SystemManager>.Debug ("Using ConsoleKit.Manager dbus service");
				}
				
			} catch (Exception e) {
				Log<SystemService>.Error ("Could not initialize needed dbus service: '{0}'", e.Message);
				Log<SystemService>.Info (e.StackTrace);
			}
		}

		void HandleCapabilitiesChanged ()
		{
			if (CapabilitiesChanged != null)
				CapabilitiesChanged (this, EventArgs.Empty);
		}

		void HandlePowerBusChanged ()
		{
			if (BusChanged != null)
				BusChanged (this, EventArgs.Empty);
		}

		void HandleRebootRequired (object sender, EventArgs e)
		{
			if (RebootRequired != null)
				RebootRequired (this, EventArgs.Empty);
		}
		
		public bool CanHibernate ()
		{
			if (upower != null) {
				return GetBoolean (upower, UPowerName, "CanHibernate") && upower.HibernateAllowed ();
			} else if (devicekit != null) {
				return GetBoolean (devicekit, DeviceKitPowerName, "CanHibernate");
			} else {
				Log<SystemManager>.Debug ("No power bus available");
			}
			return false;
		}

		public void Hibernate ()
		{
			if (upower != null) {
				if (GetBoolean (upower, UPowerName, "CanHibernate") && upower.HibernateAllowed ())
					upower.Hibernate ();
			} else if (devicekit != null) {
				if (GetBoolean (devicekit, DeviceKitPowerName, "CanHibernate"))
					devicekit.Hibernate ();
			} else {
				Log<SystemManager>.Debug ("No power bus available");
			}
		}

		public bool CanSuspend ()
		{
			if (upower != null) {
				return GetBoolean (upower, UPowerName, "CanSuspend") && upower.SuspendAllowed ();
			} else if (devicekit != null) {
				return GetBoolean (devicekit, DeviceKitPowerName, "CanSuspend");
			} else {
				Log<SystemManager>.Debug ("No power bus available");
			}
			return false;
		}

		public void Suspend ()
		{
			if (upower != null) {
				if (GetBoolean (upower, UPowerName, "CanSuspend") && upower.SuspendAllowed ())
					upower.Suspend ();
			} else if (devicekit != null) {
				if (GetBoolean (devicekit, DeviceKitPowerName, "CanSuspend"))
					devicekit.Suspend ();
			} else {
				Log<SystemManager>.Debug ("No power bus available");
			}
		}

		public bool OnBattery ()
		{
			if (upower != null) {
				return GetBoolean (upower, UPowerName, "OnBattery");
			} else if (devicekit != null) {
				return GetBoolean (devicekit, DeviceKitPowerName, "OnBattery");
			} else{
				Log<SystemManager>.Debug ("No power bus available");
			}
			return false;
		}
		
		public bool OnLowBattery ()
		{
			if (upower != null) {
				return GetBoolean (upower, UPowerName, "OnLowBattery");
			} else if (devicekit != null) {
				return false;
			} else{
				Log<SystemManager>.Debug ("No power bus available");
			}
			return false;
		}
		
		public bool CanRestart ()
		{
			if (consolekit != null) {
				return consolekit.CanRestart ();
			} else {
				Log<SystemManager>.Debug ("No consolekit bus available");
			}
			return false;
		}

		public void Restart ()
		{
			if (consolekit != null) {
				if (consolekit.CanRestart ())
					consolekit.Restart ();
			} else {
				Log<SystemManager>.Debug ("No consolekit bus available");
			}
		}

		public bool CanStop ()
		{
			if (consolekit != null) {
				return consolekit.CanStop ();
			} else {
				Log<SystemManager>.Debug ("No consolekit bus available");
			}
			return false;
		}

		public void Stop ()
		{
			if (consolekit != null) {
				if (consolekit.CanStop ())
					consolekit.Stop ();
			} else {
				Log<SystemManager>.Debug ("No consolekit bus available");
			}
		}
		
		
	}
}
