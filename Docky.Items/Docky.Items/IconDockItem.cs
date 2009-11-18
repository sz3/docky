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
using Docky.Services;

namespace Docky.Items
{


	public abstract class IconDockItem : AbstractDockItem
	{
		string icon;
		public string Icon {
			get { return icon; }
			protected set {
				if (icon == value)
					return;
				icon = value;
				
				Gtk.IconInfo info = Gtk.IconTheme.Default.LookupIcon (icon, 48, Gtk.IconLookupFlags.ForceSvg);
				if (info != null) {
					if (info.Filename != null && info.Filename.EndsWith (".svg")) {
						icon = info.Filename;
					}
					info.Dispose ();
				}
				
				QueueRedraw ();
			}
		}
		
		double shift;
		protected double HueShift {
			get { return shift; }
			set {
				if (shift == value)
					return;
				shift = value;
				QueueRedraw ();
			}
		}
		
		protected void SetIconFromGIcon (GLib.Icon gIcon)
		{
			Icon = DockServices.Drawing.IconFromGIcon (gIcon);
		}
		
		public IconDockItem ()
		{
			icon = "default";
		}
		
		protected override sealed void PaintIconSurface (DockySurface surface)
		{
			
			int iconSize = Math.Min (surface.Width, surface.Height);
			
			Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (Icon, iconSize);
			
			if (pbuf.Width != iconSize && pbuf.Height != iconSize) {
				double scale = iconSize / (double) Math.Max (pbuf.Width, pbuf.Height);
				
				Gdk.Pixbuf temp = pbuf;
				pbuf = temp.ScaleSimple ((int) (temp.Width * scale), 
				                         (int) (temp.Height * scale), 
				                                InterpType.Hyper);
				
				temp.Dispose ();
			}
			
			if (shift != 0) {
				unsafe {
					double a, r, g, b;
					byte* pixels = (byte*) pbuf.Pixels;
					for (int i = 0; i < pbuf.Height * pbuf.Width; i++) {
						r = (double) pixels[0];
						g = (double) pixels[1];
						b = (double) pixels[2];
						a = (double) pixels[3];
						
						Cairo.Color color = new Cairo.Color (r / byte.MaxValue, 
							g / byte.MaxValue, 
							b / byte.MaxValue,
							a / byte.MaxValue);
						color = color.AddHue (shift);
						
						pixels[0] = (byte) (color.R * byte.MaxValue);
						pixels[1] = (byte) (color.G * byte.MaxValue);
						pixels[2] = (byte) (color.B * byte.MaxValue);
						pixels[3] = (byte) (color.A * byte.MaxValue);
						
						pixels += 4;
					}
				}
			}
			
			Gdk.CairoHelper.SetSourcePixbuf (surface.Context, 
			                                 pbuf, 
			                                 (surface.Width - pbuf.Width) / 2, 
			                                 (surface.Height - pbuf.Height) / 2);
			surface.Context.Paint ();
			
			pbuf.Dispose ();
		}
	}
}
