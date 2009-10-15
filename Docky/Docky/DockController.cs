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

using Docky.Interface;
using Docky.Services;

namespace Docky
{


	internal class DockController
	{
		const string DefaultTheme = "Classic";
		
		IPreferences prefs;
		List<Dock> docks;
		
		public event EventHandler ThemeChanged;
		
		public IEnumerable<Dock> Docks { 
			get { return docks.AsEnumerable (); }
		}
		
		public int NumDocks {
			get { return DockNames.Count (); }
		}
		
		IEnumerable<string> ThemeContainerFolders {
			get {
				yield return Path.Combine (DockServices.System.SystemDataFolder, "themes");
				yield return Path.Combine (DockServices.System.UserDataFolder, "themes");
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
				if (!ThemeContainerFolders.Contains (value) || DockTheme == value)
					return;
				prefs.Set ("Theme", value);
				
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
			CreateDocks ();
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
			if (docks.Count >= 4)
				return null;
			
			string name = "Dock" + 1;
			for (int i = 2; DockNames.Contains (name); i++)
				name = "Dock" + i;
			
			DockNames = DockNames.Concat (new [] { name });
			
			DockPreferences dockPrefs = new DockPreferences (name);
			Dock dock = new Dock (dockPrefs);
			docks.Add (dock);
			
			return dock;
		}
		
		public bool DeleteDock (Dock dock)
		{
			if (!docks.Contains (dock) || docks.Count == 1)
				return false;
			
			docks.Remove (dock);
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
	}
}
