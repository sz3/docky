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
		
		public HoverTextManager ()
		{
		}
		
		public void SetSurfaceAtPoint (DockySurface surface, Gdk.Point point)
		{
			if (surface == currentSurface && point == currentPoint)
				return;
				
			currentSurface = surface;
			currentPoint = point;
			
			ClearWindow ();
			
			if (surface == null)
				return;
			
			window = new Gtk.Window (Gtk.WindowType.Popup);
			
			window.AppPaintable = true;
			window.AcceptFocus = false;
			window.Decorated = false;
			window.DoubleBuffered = true;
			window.KeepAbove = true;
			window.SkipPagerHint = true;
			window.SkipTaskbarHint = true;
			window.Resizable = false;
			window.CanFocus = false;
			window.TypeHint = WindowTypeHint.Tooltip;
			window.Stick ();
			
			window.SetCompositeColormap ();
			window.SetSizeRequest (surface.Width, surface.Height);
			window.ExposeEvent += HandleWindowExposeEvent;
			
			switch (Gravity) {
			case DockPosition.Top:
				window.Move (point.X - surface.Width / 2, point.Y);
				break;
			case DockPosition.Left:
				window.Move (point.X, point.Y - surface.Height / 2);
				break;
			case DockPosition.Right:
				window.Move (point.X - surface.Width, point.Y - surface.Height / 2);
				break;
			case DockPosition.Bottom:
				window.Move (point.X - surface.Width / 2, point.Y - surface.Height);
				break;
			}
			
			if (Visible)
				window.Show ();
		}

		void HandleWindowExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				cr.Operator = Operator.Source;
				cr.SetSource (currentSurface.Internal);
				cr.Paint ();
			}
		}
		
		public void Show ()
		{
			Visible = true;
			if (window != null)
				window.Show ();
		}
		
		public void Hide ()
		{
			Visible = false;
			if (window != null)
				window.Hide ();
		}
		
		void ClearWindow ()
		{
			if (window != null) {
				window.Destroy ();
				window.Dispose ();
				window = null;
			}
		}
		#region IDisposable implementation
		public void Dispose ()
		{
			ClearWindow ();
		}
		#endregion
	}
}
