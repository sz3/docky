//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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
using System.IO;
using System.Linq;
using System.Text;

using Cairo;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Clock
{


	public class ClockDockItem : AbstractDockItem
	{
		int minute;
		
		static IPreferences prefs = DockServices.Preferences.Get<ClockDockItem> ();
		
		bool show_military = prefs.Get<bool> ("ShowMilitary", false);
		bool ShowMilitary {
			get { return show_military; }
			set {
				if (show_military == value)
					return;
				show_military = value;
				prefs.Set<bool> ("ShowMilitary", value);
			}
		}
		
		bool digital = prefs.Get<bool> ("ShowDigital", false);
		bool ShowDigital {
			get { return digital; }
			set {
				if (digital == value)
					return;
				digital = value;
				prefs.Set<bool> ("ShowDigital", value);
			}
		}
		
		bool show_date = prefs.Get<bool> ("ShowDate", false);
		bool ShowDate {
			get { return show_date; }
			set {
				if (show_date == value)
					return;
				show_date = value;
				prefs.Set<bool> ("ShowDate", value);
			}
		}
		
		string current_theme = prefs.Get<string> ("ClockTheme", "default");
		public string CurrentTheme {
			get { return current_theme; }
			protected set {
				if (current_theme == value)
					return;
				current_theme = value;
				prefs.Set<string> ("ClockTheme", value);
			}
		}
		
		public IEnumerable<string> ThemeURIs {
			get {
				//yield return System.IO.Path.Combine (Services.Paths.UserDataDirectory, "ClockTheme/");
				yield return "/usr/share/gnome-do/ClockTheme/";
				yield return "/usr/local/share/gnome-do/ClockTheme/";
			}
		}
		
		string ThemePath {
			get {
				foreach (string path in ThemeURIs)
					if (Directory.Exists (path + CurrentTheme))
						return path + CurrentTheme;

				return "";
			}
		}
		
		public ClockDockItem ()
		{
			GLib.Timeout.Add (1000, ClockUpdateTimer);
		}
		
		public override string UniqueID ()
		{
			return "Clock";
		}
		
		bool ClockUpdateTimer ()
		{
			if (minute != DateTime.UtcNow.Minute) {
				QueueRedraw ();
				minute = DateTime.UtcNow.Minute;
			}
			return true;
		}
		
		void RenderFileOntoContext (Context cr, string file, int size)
		{
			if (!File.Exists (file))
				return;
			
			Gdk.Pixbuf pbuf = Rsvg.Tool.PixbufFromFileAtSize (file, size, size);
			Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, 0, 0);
			cr.Paint ();
			pbuf.Dispose ();
		}

		protected override void PaintIconSurface (DockySurface surface)
		{
			if (ShowMilitary)
				HoverText = DateTime.Now.ToString ("ddd, MMM dd HH:mm");
			else
				HoverText = DateTime.Now.ToString ("ddd, MMM dd h:mm tt");
			
			int size = Math.Min (surface.Width, surface.Height);
			
			if (ShowDigital)
				MakeDigitalIcon (surface.Context, size);
			else
				MakeAnalogIcon (surface.Context, size);
		}
		
		void MakeDigitalIcon (Context cr, int size)
		{
			// useful sizes
			int timeSize = size / 4;
			int dateSize = size / 5;
			int ampmSize = size / 5;
			int spacing = timeSize / 2;
			int center = size / 2;
			
			// shared by all text
			Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
			
			layout.FontDescription = new Gtk.Style().FontDescription;
			layout.FontDescription.Weight = Pango.Weight.Bold;
			layout.Ellipsize = Pango.EllipsizeMode.None;
			layout.Width = Pango.Units.FromPixels (size);
			
			
			// draw the time, outlined
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (timeSize);
			
			if (ShowMilitary)
				layout.SetText (DateTime.Now.ToString ("HH:mm"));
			else
				layout.SetText (DateTime.Now.ToString ("h:mm"));
			
			Pango.Rectangle inkRect, logicalRect;
			layout.GetPixelExtents (out inkRect, out logicalRect);
			
			int timeYOffset = ShowMilitary ? timeSize : timeSize / 2;
			int timeXOffset = (size - inkRect.Width) / 2;
			if (ShowDate)
				cr.MoveTo (timeXOffset, timeYOffset);
			else
				cr.MoveTo (timeXOffset, timeYOffset + timeSize / 2);
			
			Pango.CairoHelper.LayoutPath (cr, layout);
			cr.LineWidth = 3;
			cr.Color = new Cairo.Color (0, 0, 0, 0.5);
			cr.StrokePreserve ();
			cr.Color = new Cairo.Color (1, 1, 1, 0.8);
			cr.Fill ();
			
			// draw the date, outlined
			if (ShowDate) {
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (dateSize);
				
				layout.SetText (DateTime.Now.ToString ("MMM dd"));
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo ((size - inkRect.Width) / 2, size - spacing - dateSize);
				
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.LineWidth = 2.5;
				cr.Color = new Cairo.Color (0, 0, 0, 0.5);
				cr.StrokePreserve ();
				cr.Color = new Cairo.Color (1, 1, 1, 0.8);
				cr.Fill ();
			}
			
			if (!ShowMilitary) {
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (ampmSize);
				
				int yOffset = ShowDate ? center - spacing : size - spacing - ampmSize;
				
				// draw AM indicator
				layout.SetText ("am");
				cr.Color = new Cairo.Color (1, 1, 1, DateTime.Now.Hour < 12 ? 0.9 : 0.5);
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo ((center - inkRect.Width) / 2, yOffset);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
				
				// draw PM indicator
				layout.SetText ("pm");
				cr.Color = new Cairo.Color (1, 1, 1, DateTime.Now.Hour > 11 ? 0.9 : 0.5);
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo (center + (center - inkRect.Width) / 2, yOffset);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
			}
		}
		
		void MakeAnalogIcon (Context cr, int size)
		{
			int center = size / 2;
			int radius = center;
			
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-drop-shadow.svg"), radius * 2);
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-face-shadow.svg"), radius * 2);
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-face.svg"), radius * 2);
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-marks.svg"), radius * 2);
			
			cr.Translate (center, center);
			cr.Color = new Cairo.Color (.15, .15, .15);
			
			cr.LineWidth = Math.Max (1, size / 48);
			cr.LineCap = LineCap.Round;
			double minuteRotation = 2 * Math.PI * (DateTime.Now.Minute / 60.0) + Math.PI;
			cr.Rotate (minuteRotation);
			cr.MoveTo (0, radius - radius * .35);
			cr.LineTo (0, 0 - radius * .15);
			cr.Stroke ();
			cr.Rotate (0 - minuteRotation);
			
			cr.Color = new Cairo.Color (0, 0, 0);
			double hourRotation = 2 * Math.PI * (DateTime.Now.Hour / (ShowMilitary ? 24.0 : 12.0)) + 
					Math.PI + (Math.PI / (ShowMilitary ? 12.0 : 6.0)) * DateTime.Now.Minute / 60.0;
			cr.Rotate (hourRotation);
			cr.MoveTo (0, radius - radius * .5);
			cr.LineTo (0, 0 - radius * .15);
			cr.Stroke ();
			cr.Rotate (0 - hourRotation);
			
			cr.Translate (0 - center, 0 - center);
			
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-glass.svg"), radius * 2);
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-frame.svg"), radius * 2);
		}
		
		public void SetTheme (string theme)
		{
			if (string.IsNullOrEmpty (theme))
				return;
			
			Gtk.Application.Invoke (delegate {
				CurrentTheme = theme;
				QueueRedraw ();
			});
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			yield return new MenuItem (Catalog.GetString ("Digital Clock"), ShowDigital ? "gtk-apply" : "gtk-remove", (o, a) => { ShowDigital = !ShowDigital; QueueRedraw (); });
			
			yield return new MenuItem (Catalog.GetString ("24-Hour Clock"), ShowMilitary ? "gtk-apply" : "gtk-remove", (o, a) => { ShowMilitary = !ShowMilitary; QueueRedraw(); });
			
			yield return new MenuItem (Catalog.GetString ("Show Date"), ShowDate ? "gtk-apply" : "gtk-remove", (o, a) => { ShowDate = !ShowDate; QueueRedraw (); }/*, !ShowDigital*/);
			
			yield return new MenuItem (Catalog.GetString ("Select Theme"), "preferences-desktop-theme", (o, a) => { new ClockThemeSelector (this).Show (); }/*, ShowDigital*/);
		}
	}
}
