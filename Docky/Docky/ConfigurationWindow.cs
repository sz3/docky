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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Gtk;

using Docky.Interface;

namespace Docky
{


	public partial class ConfigurationWindow : Gtk.Window
	{

		DockPlacementWidget placement;
		
		public ConfigurationWindow () : base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			
			int i = 0;
			foreach (string theme in Docky.Controller.DockThemes) {
				theme_combo.AppendText (theme);
				if (Docky.Controller.DockTheme == theme) {
					theme_combo.Active = i;
				}
				i++;
			}
			
			for (int mon = 1; mon <= Screen.Default.NMonitors; mon++)
				monitor_combo.AppendText ("Monitor " + mon.ToString ());
			monitor_combo.Active = 0;
			
			configuration_widget_notebook.RemovePage (0);
			
			foreach (Dock dock in Docky.Controller.Docks) {
				configuration_widget_notebook.Add (dock.PreferencesWidget);
			}
			
			configuration_widget_notebook.Page = 
				configuration_widget_notebook.PageNum (placement.ActiveDock.PreferencesWidget);
			
			ShowAll ();
		}
		
		void UpdatePlacementWidget (int monitorNumber)
		{
			placement = new DockPlacementWidget (Docky.Controller.DocksForMonitor (monitorNumber));
			placement.ActiveDockChanged += PlacementActiveDockChanged;
			
			dock_pacement_align.Add (placement);
		
			if (placement.ActiveDock != null)
				configuration_widget_notebook.Page = 
					configuration_widget_notebook.PageNum (placement.ActiveDock.PreferencesWidget);

			ShowAll ();
		}
		
		void UpdateButtons ()
		{
			add_button.Sensitive = (Docky.Controller.PositionsAvailableForDock (monitor_combo.Active).Count () -
				Docky.Controller.DocksForMonitor (monitor_combo.Active).Count ()) > 0;
			delete_button.Sensitive = Docky.Controller.Docks.Count () > 1;
		}

		void PlacementActiveDockChanged (object sender, EventArgs e)
		{
			configuration_widget_notebook.Page = 
				configuration_widget_notebook.PageNum (placement.ActiveDock.PreferencesWidget);
		}
		
		protected override bool OnDeleteEvent (Event evnt)
		{
			Hide ();
			
			return true;
		}

		protected virtual void OnCloseButtonClicked (object sender, System.EventArgs e)
		{
			Hide ();
		}


		protected virtual void OnDeleteButtonClicked (object sender, System.EventArgs e)
		{
			configuration_widget_notebook.Remove (placement.ActiveDock.PreferencesWidget);
			Docky.Controller.DeleteDock (placement.ActiveDock);
			placement.SetDocks (Docky.Controller.DocksForMonitor (monitor_combo.Active));
			UpdateButtons ();
		}

		protected virtual void OnAddButtonClicked (object sender, System.EventArgs e)
		{
			Dock dock = Docky.Controller.CreateDock (monitor_combo.Active);
			if (dock == null)
				return;
			
			configuration_widget_notebook.Add (dock.PreferencesWidget);
			placement.SetDocks (Docky.Controller.DocksForMonitor (monitor_combo.Active));
			
			placement.ActiveDock = dock;
			
			UpdateButtons ();
		}

		protected virtual void OnThemeComboChanged (object sender, System.EventArgs e)
		{
			Docky.Controller.DockTheme = theme_combo.ActiveText;
		}
		
		protected virtual void OnMonitorComboChanged (object sender, System.EventArgs e)
		{
			if (placement != null)
				dock_pacement_align.Remove (placement);
			UpdatePlacementWidget (monitor_combo.Active);
			UpdateButtons ();
		}
	
	}
}
