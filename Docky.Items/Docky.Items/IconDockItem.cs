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
		protected string Icon {
			get { return icon; }
			set {
				if (icon == value)
					return;
				icon = value;
				QueueRedraw ();
			}
		}
		
		public IconDockItem ()
		{
			icon = "default";
		}
		
		protected override sealed void PaintIconSurface (DockySurface surface)
		{
//			surface.Context.Color = new Cairo.Color (1, 1, 1, .4);
//			surface.Context.Paint ();
//			return;
			
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
			
			Gdk.CairoHelper.SetSourcePixbuf (surface.Context, 
			                                 pbuf, 
			                                 (surface.Width - pbuf.Width) / 2, 
			                                 (surface.Height - pbuf.Height) / 2);
			surface.Context.Paint ();
			
			pbuf.Dispose ();
		}
	}
}
