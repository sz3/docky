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
using Wnck;

namespace Docky.Interface
{


	public class AutohideManager : IDisposable
	{
		public event EventHandler HiddenChanged;
		public event EventHandler DockHoveredChanged;
		
		Gdk.Rectangle dock_area, last_known_geo;
		Wnck.Screen screen;
		CursorTracker tracker;
		
		bool WindowIntersectingOther { get; set; }
		
		bool dockHoverd;
		public bool DockHovered {
			get { return dockHoverd; }
			private set { 
				if (dockHoverd == value)
					return;
				
				dockHoverd = value; 
				OnDockHoveredChanged ();
			}
		}
		
		bool hidden;
		public bool Hidden {
			get { return hidden; } 
			private set { 
				if (hidden == value)
					return;
				
				hidden = value; 
				OnHiddenChanged ();
			} 
		}
		
		AutohideType behavior;
		public AutohideType Behavior { 
			get { return behavior; } 
			set { 
				if (behavior == value)
					return;
				
				behavior = value; 
				if (behavior == AutohideType.None) {
					Hidden = false;
				}
			} 
		}
		
		internal AutohideManager (Gdk.Screen screen)
		{
			tracker = CursorTracker.ForDisplay (screen.Display);
			this.screen = Wnck.Screen.Get (screen.Number);
			
			tracker.CursorPositionChanged   += HandleCursorPositionChanged;
			this.screen.ActiveWindowChanged += HandleActiveWindowChanged;
		}
		
		public void SetDockArea (Gdk.Rectangle area)
		{
			dock_area = area;
			UpdateWindowIntersect ();
		}
		
		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs args)
		{
			DockHovered = dock_area.Contains (tracker.Cursor);
			SetHidden ();
		}

		void HandleActiveWindowChanged (object o, ActiveWindowChangedArgs args)
		{
			if (args.PreviousWindow != null)
				args.PreviousWindow.GeometryChanged -= HandleGeometryChanged;
			
			SetupActiveWindow ();
			UpdateWindowIntersect ();
			SetHidden ();
		}
		
		void SetupActiveWindow ()
		{
			Wnck.Window active = screen.ActiveWindow;
			if (active != null) {
				active.GeometryChanged += HandleGeometryChanged; 
				last_known_geo = active.EasyGeometry ();
			}
		}
		
		void HandleGeometryChanged (object sender, EventArgs e)
		{
			Wnck.Window window = sender as Wnck.Window;
			
			if (sender == null)
				return;
			
			Gdk.Rectangle geo = window.EasyGeometry ();
			
			if (geo == last_known_geo)
				return;
			
			last_known_geo = geo;
			UpdateWindowIntersect ();
			SetHidden ();
		}
		
		void UpdateWindowIntersect ()
		{
			Gdk.Rectangle adjustedDockArea = dock_area;
			adjustedDockArea.Inflate (-2, -2);
			
			bool intersect = false;
			try {
				Wnck.Window activeWindow = screen.ActiveWindow;
				
				intersect = screen.Windows.Any (w => w.WindowType != Wnck.WindowType.Desktop && 
				                                activeWindow.Pid == w.Pid &&
				                                w.EasyGeometry ().IntersectsWith (adjustedDockArea));
			} catch {
			}
			
			WindowIntersectingOther = intersect;
		}
		
		void SetHidden ()
		{
			switch (Behavior) {
			default:
			case AutohideType.None:
				Hidden = false;
				break;
			case AutohideType.Autohide:
				Hidden = !DockHovered;
				break;
			case AutohideType.Intellihide:
				Hidden = !DockHovered && WindowIntersectingOther;
				break;
			}
		}
		
		void OnDockHoveredChanged ()
		{
			if (DockHoveredChanged != null)
				DockHoveredChanged (this, EventArgs.Empty);
		}
		
		void OnHiddenChanged ()
		{
			if (HiddenChanged != null)
				HiddenChanged (this, EventArgs.Empty);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (screen != null) {
				screen.ActiveWindowChanged -= HandleActiveWindowChanged;
				screen.ActiveWindow.GeometryChanged -= HandleGeometryChanged;
			}
		}
		#endregion
	}
}
