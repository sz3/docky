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
using System.IO;
using System.Text;

using Gdk;
using Gtk;

using Mono.Unix;

using Docky.Items;
using Docky.Interface;
using Docky.Services;

namespace Docky
{
	
	class DockMonitor
	{
		public Rectangle Geo { get; set; }
		public int MonitorNumber { get; set; }
		public IEnumerable<DockPosition> PossiblePositions { get; set; }
	}

	internal class DockController : IDisposable
	{
		const string DefaultTheme = "Classic";
		
		IPreferences prefs;
		List<Dock> docks;
		
		public event EventHandler ThemeChanged;
		public event EventHandler DockHueChanged;
		
		public IEnumerable<Dock> Docks { 
			get { return docks.AsEnumerable (); }
		}
		
		public int NumDocks {
			get { return DockNames.Count (); }
		}
		
		List<DockMonitor> DockMonitors { get; set; }

		public IEnumerable<DockPosition> PositionsAvailableForDock (int monitorNum)
		{
			if (DockMonitors.Count () != Gdk.Screen.Default.NMonitors)
				DetectMonitors ();
			
			foreach (DockPosition position in DockMonitors.Where (d => d.MonitorNumber == monitorNum).First ().PossiblePositions) {
				if (!DocksForMonitor (monitorNum).Any (dock => dock.Preferences.Position == position))
					yield return position;
			}
		}

		public IEnumerable<Dock> DocksForMonitor (int monitorNumber)
		{
			return docks.Where (d => d.Preferences.MonitorNumber == monitorNumber);
		}
		
		IEnumerable<string> ThemeContainerFolders {
			get {
				yield return Path.Combine (DockServices.System.SystemDataFolder, "themes");
				yield return Path.Combine (DockServices.System.UserDataFolder, "themes");
			}
		}
		
		public int UrgentHueShift {
			get {
				return prefs.Get<int> ("UrgentHue", 150);
			}
			set {
				if (UrgentHueShift == value)
					return;
				// clamp to -180 .. 180
				int hue = Math.Max (-180, Math.Min (180, value));
				prefs.Set ("UrgentHue", hue);
			}
		}
		
		public int DockyHueShift {
			get {
				return prefs.Get<int> ("ManualDockyHue", 0);
			}
			set {
				if (DockyHueShift == value)
					return;
				// clamp to 0 .. 360
				int hue = Math.Max (0, Math.Min (360, value));
				prefs.Set ("ManualDockyHue", hue);
			}
		}
		
		public IEnumerable<string> DockThemes {
			get {
				yield return DefaultTheme;
				foreach (string dir in ThemeContainerFolders) {
					if (!Directory.Exists (dir))
						continue;
					foreach (string s in Directory.GetDirectories (dir))
						yield return Path.GetFileName (s);
				}
			}
		}
		
		public string DockTheme {
			get {
				return prefs.Get ("Theme", DefaultTheme);
			}
			set {
				if (DockTheme == value)
					return;
				prefs.Set ("Theme", value);
				Log<DockController>.Info ("Setting theme: " + value);
				if (ThemeChanged != null)
					ThemeChanged (this, EventArgs.Empty);
			}
		}
		
		public string BackgroundSvg {
			get {
				return ThemedSvg ("background.svg", "classic.svg");
			}
		}
		
		public string MenuSvg {
			get {
				return ThemedSvg ("menu.svg", "menu.svg");
			}
		}
		
		public string TooltipSvg {
			get {
				return ThemedSvg ("tooltip.svg", "tooltip.svg");
			}
		}
		
		IEnumerable<string> DockNames {
			get {
				return prefs.Get<string []> ("ActiveDocks", new [] {"Dock1"}).AsEnumerable ().Take (4);
			}
			set {
				prefs.Set<string []> ("ActiveDocks", value.ToArray ());
			}
		}
		
		public DockController ()
		{
		}
		
		public void Initialize ()
		{
			docks = new List<Dock> ();
			prefs = DockServices.Preferences.Get<DockController> ();
			
			prefs.AddNotify ("ManualDockyHue", OnDockyHueChanged);
			
			Log<DockController>.Info ("Setting theme: " + DockTheme);
			
			DetectMonitors ();
			CreateDocks ();
			
			EnforceWindowManager ();
			EnsurePluginState ();
			
			GLib.Timeout.Add (500, delegate {
				EnsurePluginState ();
				return false;
			});
		}
		
		void OnDockyHueChanged ()
		{
			if (DockHueChanged != null)
				DockHueChanged (this, EventArgs.Empty);
		}
		
		void DetectMonitors ()
		{
			DockMonitors = new List<DockMonitor> ();
			
			// first add all of the screens and their geometries
			for (int i = 0; i < Screen.Default.NMonitors; i++) {
				DockMonitor mon = new DockMonitor ();
				mon.MonitorNumber = i;
				mon.Geo = Screen.Default.GetMonitorGeometry (i);
				DockMonitors.Add (mon);
			}
			
			int topDockVal = DockMonitors.OrderBy (d => d.Geo.Top).First ().Geo.Top;
			int bottomDockVal = DockMonitors.OrderByDescending (d => d.Geo.Bottom).First ().Geo.Bottom;
			int leftDockVal = DockMonitors.OrderBy (d => d.Geo.Left).First ().Geo.Left;
			int rightDockVal = DockMonitors.OrderByDescending (d => d.Geo.Right).First ().Geo.Right;
			
			// now build the list of available positions for a given screen.
			for (int i = 0; i < DockMonitors.Count (); i++) {
				List<DockPosition> positions = new List<DockPosition> ();
				DockMonitor mon = DockMonitors.Where (d => d.MonitorNumber == i).First ();
				
				if (mon.Geo.Left == leftDockVal)
					positions.Add (DockPosition.Left);
				if (mon.Geo.Right == rightDockVal)
					positions.Add (DockPosition.Right);
				if (mon.Geo.Top == topDockVal)
					positions.Add (DockPosition.Top);
				if (mon.Geo.Bottom == bottomDockVal)
					positions.Add (DockPosition.Bottom);
				
				mon.PossiblePositions = positions;
			}
		}
		
		string FolderForTheme (string theme)
		{
			foreach (string dir in ThemeContainerFolders) {
				if (!Directory.Exists (dir))
					continue;
				foreach (string subdir in Directory.GetDirectories (dir)) {
					if (Path.GetFileName (subdir) == theme)
						return subdir;
				}
			}
			return null;
		}
		
		string ThemedSvg (string svgName, string def)
		{
			string themeFolder = FolderForTheme (DockTheme);
			
			if (DockTheme != DefaultTheme && themeFolder != null) {
				string path = Path.Combine (themeFolder, svgName);
				if (File.Exists (path))
					return path;
			}
			return def + "@" + System.Reflection.Assembly.GetExecutingAssembly ().FullName;
		}
		
		public Dock CreateDock ()
		{
			int mon;
			for (mon = 0; mon < Screen.Default.NMonitors; mon++) {
				if (PositionsAvailableForDock (mon).Any ())
					break;
				if (mon == Screen.Default.NMonitors - 1)
					return null;
			}
			
			string name = "Dock" + 1;
			for (int i = 2; DockNames.Contains (name); i++)
				name = "Dock" + i;
			
			DockNames = DockNames.Concat (new[] { name });
			
			DockPreferences dockPrefs = new DockPreferences (name, mon);
			dockPrefs.Position = PositionsAvailableForDock (mon).First ();
			Dock dock = new Dock (dockPrefs);
			docks.Add (dock);
			
			return dock;
		}
		
		public bool DeleteDock (Dock dock)
		{
			if (!docks.Contains (dock) || docks.Count == 1)
				return false;
			
			docks.Remove (dock);
			if (dock.Preferences.DefaultProvider.IsWindowManager)
				docks.First ().Preferences.DefaultProvider.SetWindowManager ();
			dock.Preferences.FreeProviders ();
			dock.Preferences.ResetPreferences ();
			dock.Dispose ();
			DockNames = DockNames.Where (s => s != dock.Preferences.GetName ());
			
			return true;
		}
		
		void CreateDocks ()
		{
			foreach (string name in DockNames) {
				DockPreferences dockPrefs = new DockPreferences (name);
				Dock dock = new Dock (dockPrefs);
				docks.Add (dock);
			}
		}
		
		void EnforceWindowManager ()
		{
			bool hasWm = false;
			
			foreach (Dock dock in docks)
				if (dock.Preferences.DefaultProvider.IsWindowManager){
					hasWm = true;
					break;
				}
			
			if (!hasWm)
				docks.First ().Preferences.DefaultProvider.SetWindowManager ();
		}
		
		void EnsurePluginState ()
		{
			foreach (AbstractDockItemProvider provider in PluginManager.ItemProviders) {
				if (!docks.Any (d => d.Preferences.ItemProviders.Contains (provider))) {
					PluginManager.Disable (provider);
				}
			}
		}
		#region IDisposable implementation
		public void Dispose ()
		{
			foreach (Dock d in Docks) {
				d.Dispose ();
			}
		}
		#endregion
	}
}
