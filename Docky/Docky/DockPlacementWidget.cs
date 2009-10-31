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


	public class DockPlacementWidget : Gtk.DrawingArea
	{
		const int Width = 260;
		const int Height = 175;
		
		const int DockSize = 30;

		Gdk.Rectangle allocation;
		IEnumerable<Dock> docks;
		
		public event EventHandler ActiveDockChanged;
		
		int X {
			get { return (allocation.Width - Width) / 2; }
		}
		
		int Y {
			get { return (allocation.Height - Height) / 2; }
		}
		
		Dock activeDock;
		public Dock ActiveDock {
			get { return activeDock; }
			set {
				if (docks.Contains (value)) {
					activeDock = value;
					OnActiveDockChanged ();
				}
				QueueDraw ();
			}
		}
		
		public DockPlacementWidget (IEnumerable<Dock> docks)
		{
			this.docks = docks;
			RegisterDocks ();
			if (docks.Any ())
				ActiveDock = docks.First ();
			else
				ActiveDock = null;
			SetSizeRequest (Width, Height);
			
			AddEvents ((int) Gdk.EventMask.AllEventsMask);
		}
		
		public void SetDocks (IEnumerable<Dock> docks)
		{
			UnregisterDocks ();
			this.docks = docks;
			RegisterDocks ();
			if (docks.Any ())
				ActiveDock = docks.First ();
			else
				ActiveDock = null;
		}
		
		void RegisterDocks ()
		{
			foreach (Dock dock in docks) {
				dock.Preferences.PositionChanged += DockPreferencesPositionChanged;
			}
		}

		void DockPreferencesPositionChanged (object sender, EventArgs e)
		{
			QueueDraw ();
		}
		
		void UnregisterDocks ()
		{
			foreach (Dock dock in docks) {
				dock.Preferences.PositionChanged -= DockPreferencesPositionChanged;
			}
		}
		
		// Is there really not an easier way to get this?
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			this.allocation = allocation;
			
			base.OnSizeAllocated (allocation);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			foreach (Dock dock in docks) {
				Gdk.Rectangle area = DockRenderArea (dock.Preferences.Position);
				if (area.Contains ((int) evnt.X, (int) evnt.Y)) {
					ActiveDock = dock;
					OnActiveDockChanged ();
					break;
				}
			}
			
			return base.OnButtonReleaseEvent (evnt);
		}

		
		Gdk.Rectangle DockRenderArea (DockPosition position)
		{
			int x = X;
			int y = Y;
			
			switch (position) {
			case DockPosition.Top:
				return new Gdk.Rectangle (x + (DockSize + 5), y + 1, Width - 2 * (DockSize + 5), DockSize);
			case DockPosition.Left:
				return new Gdk.Rectangle (x + 1, y + (DockSize + 5), DockSize, Height - 2 * (DockSize + 5));
			case DockPosition.Right:
				return new Gdk.Rectangle (x + Width - DockSize - 1, y + (DockSize + 5), DockSize, Height - 2 * (DockSize + 5)); 
			default:
			case DockPosition.Bottom:
				return new Gdk.Rectangle (x + (DockSize + 5), y + Height - DockSize - 1, Width - 2 * (DockSize + 5), DockSize);
			}
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return true;
			
			bool result = base.OnExposeEvent (evnt);
			
			int x = X;
			int y = Y;
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.RoundedRectangle (x + .5, 
				                     y + .5, 
				                     Width - 1, 
				                     Height - 1,
				                     5);
				
				LinearGradient lg = new LinearGradient (x, y, x + Width, y + Width);
				lg.AddColorStop (0, new Cairo.Color (0.6, 0.9, 1.0, 0.7));
				lg.AddColorStop (1, new Cairo.Color (0.3, 0.6, 0.9, 0.7));
				cr.Pattern = lg;
				cr.FillPreserve ();
				
				lg.Destroy ();
				
				cr.Color = new Cairo.Color (0, 0, 0);
				cr.LineWidth = 1;
				cr.Stroke ();
				
				foreach (Dock dock in docks) {
					Gdk.Rectangle area = DockRenderArea (dock.Preferences.Position);
					cr.Rectangle (area.X, area.Y, area.Width, area.Height);
					
					if (ActiveDock == dock)
						cr.Color = new Cairo.Color (1, 1, 1, .9);
					else
						cr.Color = new Cairo.Color (1, 1, 1, .35);
					
					cr.Fill ();
				}
			}
			
			return result;
		}

		void OnActiveDockChanged ()
		{
			QueueDraw ();
			if (ActiveDockChanged != null)
				ActiveDockChanged (this, EventArgs.Empty);
		}
	}
}
