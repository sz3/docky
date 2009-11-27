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

using Docky.CairoHelper;
using Docky.Items;

namespace Docky.Interface
{

	public class HoverTextManager : IDisposable
	{

		public DockPosition Gravity { get; set; }
		public bool Visible { get; private set; }
		
		Gtk.Window window;
		Gdk.Point currentPoint;
		DockySurface currentSurface;
		uint timer;
		bool fullscreen;
		
		public HoverTextManager ()
		{
			window = new Gtk.Window (Gtk.WindowType.Popup);
			
			window.AppPaintable = true;
			window.AcceptFocus = false;
			window.Decorated = false;
			window.DoubleBuffered = true;
			window.SkipPagerHint = true;
			window.SkipTaskbarHint = true;
			window.Resizable = false;
			window.CanFocus = false;
			window.TypeHint = WindowTypeHint.Tooltip;
			window.Stick ();
			
			window.SetCompositeColormap ();
			window.ExposeEvent += HandleWindowExposeEvent;
			
			Wnck.Screen.Default.ActiveWindowChanged += HandleWnckScreenDefaultActiveWindowChanged;
		}

		void HandleWnckScreenDefaultActiveWindowChanged (object o, Wnck.ActiveWindowChangedArgs args)
		{
			if (args.PreviousWindow != null)
				args.PreviousWindow.GeometryChanged -= HandleWnckScreenDefaultActiveWindowGeometryChanged;
			if (Wnck.Screen.Default.ActiveWindow != null)
				Wnck.Screen.Default.ActiveWindow.GeometryChanged += HandleWnckScreenDefaultActiveWindowGeometryChanged;
		}

		void HandleWnckScreenDefaultActiveWindowGeometryChanged (object sender, EventArgs e)
		{
			Wnck.Window active = sender as Wnck.Window;
			if (active == null)
				return;

			fullscreen = active.IsFullscreen;
			if (fullscreen && window != null)
				window.Hide ();
		}
		
		public void SetSurfaceAtPoint (DockySurface surface, Gdk.Point point)
		{
			if (surface == currentSurface && point == currentPoint) {
				window.QueueDraw ();
				return;
			}
			
			currentSurface = surface;
			currentPoint = point;
			
			if (surface == null) {
				window.Hide ();
				return;
			}
			
			window.SetSizeRequest (surface.Width, surface.Height);
				
			Gdk.Point center = Gdk.Point.Zero;
			int padding = 5;
			switch (Gravity) {
			case DockPosition.Top:
				center = new Gdk.Point (point.X - surface.Width / 2, point.Y + padding);
				break;
			case DockPosition.Left:
				center = new Gdk.Point (point.X + padding, point.Y - surface.Height / 2);
				break;
			case DockPosition.Right:
				center = new Gdk.Point (point.X - surface.Width - padding, point.Y - surface.Height / 2);
				break;
			case DockPosition.Bottom:
				center = new Gdk.Point (point.X - surface.Width / 2, point.Y - surface.Height - padding);
				break;
			}
			
			if (timer > 0)
				GLib.Source.Remove (timer);
			
			window.Move (center.X, center.Y);
			timer = GLib.Timeout.Add (100, delegate {
				window.Move (center.X, center.Y);
				window.QueueDraw ();
				timer = 0;
				return false;
			});
			
			if (Visible && !fullscreen)
				window.Show ();
		}

		void HandleWindowExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				cr.Operator = Operator.Source;
				
				if (currentSurface == null)
					cr.Color = new Cairo.Color (1, 1, 1, 0);
				else
					cr.SetSource (currentSurface.Internal);
				
				cr.Paint ();
			}
		}
		
		public void Show ()
		{
			Visible = true;
			if (currentSurface != null && !fullscreen)
				window.Show ();
		}
		
		public void Hide ()
		{
			Visible = false;
			window.Hide ();
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			currentSurface = null;
			Wnck.Screen.Default.ActiveWindowChanged -= HandleWnckScreenDefaultActiveWindowChanged;
			if (window != null) {
				window.Destroy ();
				window.Dispose ();
				window = null;
			}
		}
		#endregion
	}
}
