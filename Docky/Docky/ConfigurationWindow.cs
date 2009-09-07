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
		
		public ConfigurationWindow () : base (Gtk.WindowType.Toplevel)
		{
			this.Build ();
			
			placement = new DockPlacementWidget ();
			placement.ActiveDockChanged += PlacementActiveDockChanged;
			
			dock_pacement_align.Add (placement);
			
			configuration_widget_notebook.RemovePage (0);
			
			foreach (Dock dock in Docky.Controller.Docks) {
				configuration_widget_notebook.Add (dock.PreferencesWidget);
			}
			
			ShowAll ();
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

	}
}
