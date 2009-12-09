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
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Gdk;
using Gtk;
using Cairo;
using Mono.Unix;

using Docky.Services;
using Docky.CairoHelper;

namespace Docky.Items
{

	public abstract class IconDockItem : AbstractDockItem
	{
		public event EventHandler IconUpdated;
		
		string icon;
		public string Icon {
			get { return icon; }
			protected set {
				if (icon == value)
					return;
				// if we set this, clear the forced pixbuf
				if (forced_pixbuf != null)
					forced_pixbuf = null;
				icon = value;
				
				using (Gtk.IconInfo info = Gtk.IconTheme.Default.LookupIcon (icon, 48, Gtk.IconLookupFlags.ForceSvg)) {
					if (info != null && info.Filename != null && info.Filename.EndsWith (".svg")) {
						icon = info.Filename;
						ScalableRendering = true;
					} else {
						ScalableRendering = false;
					}
				}
				
				OnIconUpdated ();
				QueueRedraw ();
			}
		}
		
		Pixbuf forced_pixbuf;
		protected Pixbuf ForcePixbuf {
			get { return forced_pixbuf; }
			set {
				if (forced_pixbuf == value)
					return;
				forced_pixbuf = value;
				QueueRedraw ();
			}
		}
		
		protected void SetIconFromGIcon (GLib.Icon gIcon)
		{
			Icon = DockServices.Drawing.IconFromGIcon (gIcon);
		}
		
		protected void SetIconFromPixbuf (Pixbuf pbuf)
		{
			forced_pixbuf = pbuf;
		}
		
		public IconDockItem ()
		{
			Icon = "";
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{			
			Gdk.Pixbuf pbuf;
			
			if (forced_pixbuf == null)
				pbuf = DockServices.Drawing.LoadIcon (Icon, surface.Width, surface.Height);
			else
				pbuf = DockServices.Drawing.ARScale (surface.Width, surface.Height, forced_pixbuf);

			Gdk.CairoHelper.SetSourcePixbuf (surface.Context, 
			                                 pbuf, 
			                                 (surface.Width - pbuf.Width) / 2, 
			                                 (surface.Height - pbuf.Height) / 2);
			surface.Context.Paint ();
			
			pbuf.Dispose ();
			
			try {
				PostProcessIconSurface (surface);
			} catch (Exception e) {
				Log<IconDockItem>.Error (e.Message);
				Log<IconDockItem>.Debug (e.StackTrace);
			}
		}
		
		protected virtual void PostProcessIconSurface (DockySurface surface)
		{
		}
		
		protected void OnIconUpdated ()
		{
			if (IconUpdated != null)
				IconUpdated (this, EventArgs.Empty);
		}
	}
}
