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

using Docky.Items;

namespace Docky.Interface
{


	public class DockWindow : Gtk.Window
	{
		DateTime hidden_change_time;
		DateTime cursor_over_dock_area_change_time;
		
		IDockPreferences preferences;
		
		IDockPreferences Preferences { 
			get { return preferences; }
			set {
				if (preferences == value)
					return;
				UnregisterPreferencesEvents (preferences);
				preferences = value;
				RegisterPreferencesEvents (value);
			}
		}
		
		#region Preferences Shortcuts
		IEnumerable<IDockItemProvider> ItemProviders {
			get { return Preferences.ItemProviders; }
		}
		
		int IconSize {
			get { return Preferences.IconSize; }
		}
		
		DockPosition Position {
			get { return Preferences.Position; }
		}
		
		bool ZoomEnabled {
			get { return Preferences.ZoomEnabled; }
		}
		
		double ZoomPercent {
			get { return Preferences.ZoomPercent; }
		}
		#endregion
		
		AutohideManager AutohideManager { get; set; }
		
		CursorTracker CursorTracker { get; set; }
		
		public DockWindow () : base (Gtk.WindowType.Toplevel)
		{
			AppPaintable    = true;
			AcceptFocus     = false;
			Decorated       = false;
			DoubleBuffered  = false;
			SkipPagerHint   = true;
			SkipTaskbarHint = true;
			Resizable       = false;
			CanFocus        = false;
			TypeHint        = WindowTypeHint.Dock;
			
			SetCompositeColormap ();
			Stick ();
			
			Realized += HandleRealized;	
		}

		void HandleRealized (object sender, EventArgs e)
		{
			CursorTracker = CursorTracker.ForDisplay (Display);
			CursorTracker.CursorPositionChanged += HandleCursorPositionChanged;	
			
			AutohideManager = new AutohideManager (Screen);
			AutohideManager.Behavior = Preferences.Autohide;
			AutohideManager.HiddenChanged += AutohideManagerHiddenChanged;
			AutohideManager.CursorIsOverDockAreaChanged += AutohideManagerCursorIsOverDockAreaChanged;
		}

		void AutohideManagerCursorIsOverDockAreaChanged (object sender, EventArgs e)
		{
			cursor_over_dock_area_change_time = DateTime.UtcNow;
		}

		void AutohideManagerHiddenChanged (object sender, EventArgs e)
		{
			hidden_change_time = DateTime.UtcNow;
		}

		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs e)
		{
			
		}
		
		void RegisterPreferencesEvents (IDockPreferences preferences)
		{
			preferences.AutohideChanged    += PreferencesAutohideChanged;	
			preferences.IconSizeChanged    += PreferencesIconSizeChanged;	
			preferences.PositionChanged    += PreferencesPositionChanged;
			preferences.ZoomEnabledChanged += PreferencesZoomEnabledChanged;
			preferences.ZoomPercentChanged += PreferencesZoomPercentChanged;		
		}
		
		void UnregisterPreferencesEvents (IDockPreferences preferences)
		{
			preferences.AutohideChanged    -= PreferencesAutohideChanged;	
			preferences.IconSizeChanged    -= PreferencesIconSizeChanged;	
			preferences.PositionChanged    -= PreferencesPositionChanged;
			preferences.ZoomEnabledChanged -= PreferencesZoomEnabledChanged;
			preferences.ZoomPercentChanged -= PreferencesZoomPercentChanged;		
		}

		void PreferencesZoomPercentChanged (object sender, EventArgs e)
		{
			
		}

		void PreferencesZoomEnabledChanged (object sender, EventArgs e)
		{
			
		}

		void PreferencesPositionChanged (object sender, EventArgs e)
		{
			
		}

		void PreferencesIconSizeChanged (object sender, EventArgs e)
		{
			
		}

		void PreferencesAutohideChanged (object sender, EventArgs e)
		{
			
		}
		
		void SetCompositeColormap ()
		{
			Gdk.Colormap colormap;

            colormap = Screen.RgbaColormap;
            if (colormap == null) {
                    colormap = Screen.RgbColormap;
                    Console.Error.WriteLine ("No alpha support.");
            }
            
            Colormap = colormap;
            colormap.Dispose ();
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return true;
			
			return false;
		}
	}
}
