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

using Docky.Items;

namespace Docky.Interface
{


	[System.ComponentModel.ToolboxItem(true)]
	public partial class DockPreferences : Gtk.Bin, IDockPreferences
	{
		string name;
		int icon_size;
		bool zoom_enabled;
		double zoom_percent;
		DockPosition position;
		AutohideType hide_type;
		
		List<IDockItemProvider> item_providers;
		
		public event EventHandler PositionChanged;
		public event EventHandler IconSizeChanged;
		public event EventHandler AutohideChanged;
		public event EventHandler ZoomEnabledChanged;
		public event EventHandler ZoomPercentChanged;
		
		public IEnumerable<IDockItemProvider> ItemProviders { 
			get { item_providers.AsEnumerable (); }
		}
		
		public ApplicationDockItemProvider ApplicationProvider { get; private set; }
		
		public AutohideType Autohide {
			get { return hide_type; }
			set {
				if (hide_type == value)
					return;
				hide_type = value;
				OnAutohideChanged ();
			}
		}
		
		public DockPosition Position {
			get { return position; }
			set {
				if (position == value)
					return;
				position = value;
				OnPositionChanged ();
			}
		}
		
		public int IconSize {
			get { return icon_size; }
			set {
				if (icon_size == value)
					return;
				icon_size = value;
				OnIconSizeChanged ();
			}
		}
		
		public bool ZoomEnabled {
			get { return zoom_enabled; }
			set {
				if (zoom_enabled == value)
					return;
				zoom_enabled = value;
				OnZoomEnabledChanged ();
			}
		}
				
		public double ZoomPercent {
			get { return zoom_percent; }
			set {
				if (zoom_percent == value)
					return;
				zoom_percent = value;
				OnZoomPercentChanged ();
			}
		}
		
		public DockPreferences (string dockName)
		{
			this.Build ();
			name = dockName;
			BuildOptions ();
		}
		
		public bool SetName (string name)
		{
			
			return true;
		}
		
		public string GetName ()
		{
			return name;
		}
		
		void BuildOptions ()
		{
			
		}
		
		void BuildItemProviders ()
		{
			item_providers = new List<IDockItemProvider> ();
			
			ApplicationProvider = new ApplicationDockItemProvider ();
			item_providers.Add (item_providers);
		}
		
		void OnAutohideChanged ()
		{
			if (AutohideChanged != null)
				AutohideChanged (this, EventArgs.Empty);
		}
		
		void OnPositionChanged ()
		{
			if (PositionChanged != null)
				PositionChanged (this, EventArgs.Empty);
		}
		
		void OnIconSizeChanged ()
		{
			if (IconSizeChanged != null)
				IconSizeChanged (this, EventArgs.Empty);
		}
				
		void OnZoomEnabledChanged ()
		{
			if (ZoomEnabledChanged != null)
				ZoomEnabledChanged (this, EventArgs.Empty);
		}
		
		void OnZoomPercentChanged ()
		{
			if (ZoomPercentChanged != null)
				ZoomPercentChanged (this, EventArgs.Empty);
		}
	}
}
