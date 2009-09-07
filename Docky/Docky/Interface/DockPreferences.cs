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

using Docky.Items;
using Docky.Services;

namespace Docky.Interface
{


	[System.ComponentModel.ToolboxItem(true)]
	public partial class DockPreferences : Gtk.Bin, IDockPreferences
	{
		static T Clamp<T> (T value, T max, T min)
		where T : IComparable<T> {     
			T result = value;
			if (value.CompareTo (max) > 0)
				result = max;
			if (value.CompareTo (min) < 0)
				result = min;
			return result;
		} 
		
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
				value = Clamp (value, 128, 24);
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
				value = Clamp (value, 4, 1);
				if (zoom_percent == value)
					return;
				
				zoom_percent = value;
				SetOption<double> ("ZoomPercent", zoom_percent);
				OnZoomPercentChanged ();
			}
		}
		
		public DockPreferences (string dockName)
		{
			this.Build ();
			
			icon_scale.Adjustment.SetBounds (24, 129, 1, 1, 1);
			zoom_scale.Adjustment.SetBounds (1, 4.01, .01, .01, .01);
			
			zoom_scale.FormatValue += delegate(object o, FormatValueArgs args) {
				args.RetVal = string.Format ("{0:#}%", args.Value * 100);
			};
			
			name = dockName;
			
			prefs = DockServices.Preferences.Get<DockPreferences> ();
			
			BuildOptions ();
			BuildItemProviders ();
			
			icon_scale.ValueChanged += IconScaleValueChanged;
			zoom_scale.ValueChanged += ZoomScaleValueChanged;
			zoom_checkbutton.Toggled += ZoomCheckbuttonToggled;
			autohide_box.Changed += AutohideBoxChanged;
			position_box.Changed += PositionBoxChanged;
			
			ShowAll ();
		}

		void PositionBoxChanged (object sender, EventArgs e)
		{
			Position = (DockPosition) position_box.Active;
			
			if (position_box.Active != (int) Position)
				position_box.Active = (int) Position;
		}

		void AutohideBoxChanged (object sender, EventArgs e)
		{
			Autohide = (AutohideType) autohide_box.Active;
			
			if (autohide_box.Active != (int) Autohide)
				autohide_box.Active = (int) Autohide;
		}

		void ZoomCheckbuttonToggled (object sender, EventArgs e)
		{
			ZoomEnabled = zoom_checkbutton.Active;
			
			// may seem odd but its just a check
			zoom_checkbutton.Active = ZoomEnabled;
		}

		void ZoomScaleValueChanged (object sender, EventArgs e)
		{
			ZoomPercent = zoom_scale.Value;
			
			if (ZoomPercent != zoom_scale.Value)
				zoom_scale.Value = ZoomPercent;
		}

		void IconScaleValueChanged (object sender, EventArgs e)
		{
			IconSize = (int) icon_scale.Value;
			
			if (IconSize != icon_scale.Value)
				icon_scale.Value = IconSize;
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
			ZoomPercent = GetOption ("ZoomPercent", 2.0);
			
			position_box.Active = (int) Position;
			autohide_box.Active = (int) Autohide;
			
			zoom_checkbutton.Active = ZoomEnabled;
			zoom_scale.Value = ZoomPercent;
			icon_scale.Value = IconSize;
		}
		
		T GetOption<T> (string key, T def)
		{
			return prefs.Get<T> (name + "/" + key, def);
		}
		
		bool SetOption<T> (string key, T val)
		{
			return prefs.Set<T> (name + "/" + key, val);
		}
		
		void BuildItemProviders ()
		{
			item_providers = new List<IDockItemProvider> ();
			
			ApplicationProvider = new ApplicationDockItemProvider ();
			item_providers.Add (ApplicationProvider);
			
			ApplicationProvider.InsertItem ("/usr/share/applications/banshee-1.desktop");
			ApplicationProvider.InsertItem ("/usr/share/applications/gnome-terminal.desktop");
			ApplicationProvider.InsertItem ("/usr/share/applications/pidgin.desktop");
			ApplicationProvider.InsertItem ("/usr/share/applications/xchat.desktop");
			ApplicationProvider.InsertItem ("/usr/share/applications/firefox.desktop");
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
