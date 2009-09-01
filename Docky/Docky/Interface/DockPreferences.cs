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
using Docky.Services;

namespace Docky.Interface
{


	[System.ComponentModel.ToolboxItem(true)]
	public partial class DockPreferences : Gtk.Bin, IDockPreferences
	{
		IPreferences prefs;
		
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
		
		public event EventHandler<ItemProvidersChangedEventArgs> ItemProvidersChanged;
		
		public IEnumerable<IDockItemProvider> ItemProviders { 
			get { return item_providers.AsEnumerable (); }
		}
		
		public ApplicationDockItemProvider ApplicationProvider { get; private set; }
		
		public AutohideType Autohide {
			get { return hide_type; }
			set {
				if (hide_type == value)
					return;
				hide_type = value;
				SetOption ("Autohide", hide_type.ToString ());
				OnAutohideChanged ();
			}
		}
		
		public DockPosition Position {
			get { return position; }
			set {
				if (position == value)
					return;
				position = value;
				SetOption ("Position", position.ToString ());
				OnPositionChanged ();
			}
		}
		
		public int IconSize {
			get { return icon_size; }
			set {
				if (icon_size == value)
					return;
				icon_size = value;
				SetOption ("IconSize", icon_size);
				OnIconSizeChanged ();
			}
		}
		
		public bool ZoomEnabled {
			get { return zoom_enabled; }
			set {
				if (zoom_enabled == value)
					return;
				zoom_enabled = value;
				SetOption ("ZoomEnabled", zoom_enabled);
				OnZoomEnabledChanged ();
			}
		}
				
		public double ZoomPercent {
			get { return zoom_percent; }
			set {
				if (zoom_percent == value)
					return;
				zoom_percent = value;
				SetOption ("ZoomPercent", zoom_percent);
				OnZoomPercentChanged ();
			}
		}
		
		public DockPreferences (string dockName)
		{
			this.Build ();
			name = dockName;
			
			prefs = DockServices.Preferences.Get<DockPreferences> ();
			
			BuildOptions ();
			BuildItemProviders ();
		}
		
		public bool SetName (string name)
		{
			
			return false;
		}
		
		public string GetName ()
		{
			return name;
		}
		
		void BuildOptions ()
		{
			Autohide = (AutohideType) Enum.Parse (typeof (AutohideType), 
			                                      GetOption ("Autohide", AutohideType.None.ToString ()));
			
			DockPosition position = (DockPosition) Enum.Parse (typeof (DockPosition), 
			                                                   GetOption ("Position", DockPosition.Bottom.ToString ()));
			while (Docky.Controller.Docks.Any ((Dock d) => d.Preferences.Position == position)) {
				Console.Error.WriteLine ("Dock position already in use: " + position.ToString ());
				position = (DockPosition) (((int) position) + 1 % 4);
			}
			Position = position;
			
			IconSize    = GetOption ("IconSize", 64);
			ZoomEnabled = GetOption ("ZoomEnabled", true);
			ZoomPercent = GetOption ("ZoomPercent", 2);
		}
		
		T GetOption<T> (string key, T def)
		{
			return prefs.Get (name + "/" + key, def);
		}
		
		bool SetOption<T> (string key, T val)
		{
			return prefs.Set (name + "/" + key, val);
		}
		
		void BuildItemProviders ()
		{
			item_providers = new List<IDockItemProvider> ();
			
			ApplicationProvider = new ApplicationDockItemProvider ();
			item_providers.Add (ApplicationProvider);
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
