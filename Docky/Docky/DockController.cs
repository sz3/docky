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

using Cairo;
using Gdk;
using Gtk;

using Docky.Interface;
using Docky.Services;

namespace Docky
{


	internal class DockController
	{
		IPreferences prefs;
		List<Dock> docks;
		
		public IEnumerable<Dock> Docks { 
			get { return docks.AsEnumerable (); }
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
		
		public Dock CreateDock ()
		{
			if (docks.Count >= 4)
				return null;
			
			string name = "Dock" + 1;
			for (int i = 2; DockNames.Contains (name); i++)
				name = "Dock" + i;
			
			DockPreferences dockPrefs = new DockPreferences (name);
			Dock dock = new Dock (dockPrefs);
			docks.Add (dock);
			
			DockNames = DockNames.Concat (new [] { name });
			
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
