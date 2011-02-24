//  
//  Copyright (C) 2011 Florian Dorn
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Cairo;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Widgets;

namespace NetworkMonitorDocklet 
{
	public class NetworkMonitorDockItem : AbstractDockItem
	{
		uint timer;
		NetworkMonitor monitor;
		DeviceInfo device;
		
		public override string UniqueID () { return "NetworkMonitor"; }

		public NetworkMonitorDockItem ()
		{
			monitor = new NetworkMonitor ();
			UpdateUtilization ();
			timer = GLib.Timeout.Add (3000, UpdateUtilization);
		}
		
		bool UpdateUtilization ()
		{
			monitor.update ();
			QueueRedraw ();
			return true;
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			device = monitor.getDevice (OutputDevice.AUTO);
			HoverText = device.ToString ();
			
			Context cr = surface.Context;
			
			int fontSize = surface.Height / 5;
			
			// shared by all text
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				layout.FontDescription = new Gtk.Style ().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (surface.Width);
				
				// draw up/down
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (fontSize);
				string text = string.Format ("↓{1}\n↑{0}", device.formatUpDown (true), device.formatUpDown (false));
				
				layout.SetText (text );
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				
				int yOffset = fontSize / 2;
				int xOffset = (surface.Width - inkRect.Width) / 2;
				cr.MoveTo (xOffset, yOffset);
				
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.LineWidth = 2;
				cr.Color = new Cairo.Color (0, 0, 0, 0.5);
				cr.StrokePreserve ();
				cr.Color = new Cairo.Color (1, 1, 1, 0.8);
				cr.Fill ();
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		public override void Dispose ()
		{
			if (timer > 0)
				GLib.Source.Remove (timer);
			base.Dispose ();
		}
	}
}
