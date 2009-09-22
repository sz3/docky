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

using Docky.Services;

namespace Docky.Menus
{


	internal class MenuItemWidget : Gtk.EventBox
	{
		MenuItem item;
		bool hovered;
		
		internal MenuItemWidget (MenuItem item) : base()
		{
			this.item = item;
			item.IconChanged += ItemIconChanged;
			item.TextChanged += ItemTextChanged;
			
			AddEvents ((int) Gdk.EventMask.AllEventsMask);
			
			HasTooltip = true;
			VisibleWindow = false;
			AboveChild = true;
			
			SetSizeRequest (180, 24);
		}

		void ItemTextChanged (object sender, EventArgs e)
		{
			QueueDraw ();
		}

		void ItemIconChanged (object sender, EventArgs e)
		{
			QueueDraw ();
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			item.SendClick ();
			return base.OnButtonReleaseEvent (evnt);
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			hovered = true;
			QueueDraw ();
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			hovered = false;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnQueryTooltip (int x, int y, bool keyboard_tooltip, Tooltip tooltip)
		{
			tooltip.Text = item.Text;
			return base.OnQueryTooltip (x, y, keyboard_tooltip, tooltip);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return false;
			
			Gdk.Rectangle allocation = Allocation;
			
			Gdk.Pixbuf pixbuf = DockServices.Drawing.LoadIcon (item.Icon, allocation.Height - 6);
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				if (hovered) {
					cr.Rectangle (allocation.X, allocation.Y, allocation.Width, allocation.Height);
					cr.Color = new Cairo.Color (1, 1, 1, .1);
					cr.Fill ();
				}
				
				Gdk.CairoHelper.SetSourcePixbuf (cr, pixbuf, allocation.X + 1, allocation.Y + 3);
				cr.Paint ();
			
				Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
				layout.SetText (item.Text);
				layout.Width = Pango.Units.FromPixels (allocation.Width - allocation.Height - 10);
				layout.FontDescription = Style.FontDescription;
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (12);
				layout.FontDescription.Weight = Pango.Weight.Bold;
				
				Pango.Rectangle logical, ink;
				layout.GetPixelExtents (out ink, out logical);
				
				cr.MoveTo (allocation.X + allocation.Height + 5, allocation.Y + (allocation.Height - logical.Height) / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Color = new Cairo.Color (1, 1, 1);
				cr.Fill ();
			}
			
			pixbuf.Dispose ();
			return true;
		}
	}
}
