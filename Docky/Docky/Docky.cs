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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;

using Mono.Unix;

using Gdk;
using Gtk;

using Docky.DBus;
using Docky.Windowing;
using Docky.Services;

namespace Docky
{


	public static class Docky
	{

		public static UserArgs CommandLinePreferences { get; private set; }
		
		static DockController controller;
		internal static DockController Controller { 
			get {
				if (controller == null)
					controller = new DockController ();
				return controller;
			}
		}
		
		static ConfigurationWindow config;
		internal static ConfigurationWindow Config {
			get {
				if (config == null)
					config = new ConfigurationWindow ();
				return config;
			}
		}
		
		public static void Main (string[] args)
		{
			//Init gtk and GLib related
			Catalog.Init ("docky", AssemblyInfo.LocaleDirectory);
			Gdk.Threads.Init ();
			NDesk.DBus.BusG.Init ();
			Gtk.Application.Init ("Docky", ref args);
			Gnome.Vfs.Vfs.Initialize ();
			GLib.GType.Init ();
			
			Wnck.Global.ClientType = Wnck.ClientType.Pager;
			
			// output the version number & system info
			Log.DisplayLevel = LogLevel.Info;
			Log.Info ("Docky version: {0}", AssemblyInfo.VersionDetails);
			Log.Info ("Kernel version: {0}", System.Environment.OSVersion.Version);
			Log.Info ("CLR version: {0}", System.Environment.Version);
			
			// process the command line args
			CommandLinePreferences = new UserArgs (args);
			
			// set process name
			DockServices.System.SetProcessName ("docky");
			
			// check compositing
			CheckComposite ();
			Gdk.Screen.Default.CompositedChanged += delegate {
				CheckComposite ();
			};
			
			DBusManager.Default.Initialize ();
			PluginManager.Initialize ();
			Controller.Initialize ();
			
			
			GLib.Idle.Add (delegate {
				LaunchHelpers ();
				return false;
			});
			
			Gdk.Threads.Enter ();
			Gtk.Application.Run ();
			Gdk.Threads.Leave ();
			
			Controller.Dispose ();
			PluginManager.Shutdown ();
			Gnome.Vfs.Vfs.Shutdown ();
		}
		
		static void LaunchHelpers ()
		{
			string directory = Path.Combine (DockServices.System.SystemDataFolder, "helpers");
			
			if (!Directory.Exists (directory))
				return;
			
			foreach (string file in Directory.GetFiles (directory))
				DockServices.System.Execute (file);
		}
		
		static void CheckComposite ()
		{
			if (!Gdk.Screen.Default.IsComposited)
				Log.Notify (Catalog.GetString ("Docky requires compositing to work properly.  Please enable compositing and restart docky."));
		}
		
		public static void ShowAbout ()
		{
			Gtk.AboutDialog about = new Gtk.AboutDialog ();
			about.ProgramName = "Docky";
			about.Version = AssemblyInfo.DisplayVersion + "\n" + AssemblyInfo.VersionDetails;
			about.IconName = "docky";
			about.LogoIconName = "docky";
			about.Website = "http://launchpad.net/docky";
			about.WebsiteLabel = "Website";
			Gtk.AboutDialog.SetUrlHook ((dialog, link) => DockServices.System.Open (link));
			about.Copyright = "Copyright \xa9 2009 Docky Developers";
			about.Comments = "Docky. Simply Powerful.";
			about.Authors = new[] {
				"Jason Smith <jassmith@gmail.com>",
				"Robert Dyer <psybers@gmail.com>",
				"Chris Szikszoy <chris@szikszoy.com>"
			};
			about.Artists = new[] { 
				"Daniel Foré <daniel.p.fore@gmail.com>",
			};
			
			about.ShowAll ();
			
			about.Response += delegate {
				about.Hide ();
				about.Destroy ();
			};
			
		}
		
		public static void Quit ()
		{
			DBusManager.Default.Shutdown ();
			Gtk.Application.Quit ();
		}
	}
}
