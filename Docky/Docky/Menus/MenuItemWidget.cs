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
		const int MinWidth = 100;
		const int MaxWidth = 350;
		const int FontSize = 11;
		const int IconBuffer = 3;
		
		MenuItem item;
		bool hovered;
		
		public Cairo.Color TextColor { get; set; }
		
		public int TextWidth { get; protected set; }
		
		internal MenuItemWidget (MenuItem item) : base()
		{
			TextColor = new Cairo.Color (1, 1, 1);
			this.item = item;
			item.IconChanged += ItemIconChanged;
			item.TextChanged += ItemTextChanged;
			item.DisabledChanged += ItemDisabledChanged;
			
			AddEvents ((int) Gdk.EventMask.AllEventsMask);
			
			HasTooltip = true;
			VisibleWindow = false;
			AboveChild = true;
			
			CalcTextWidth ();
		}
		
		void CalcTextWidth ()
		{
			Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
			layout.SetText (item.Text);
			layout.Width = Pango.Units.FromPixels (2 * MaxWidth);
			layout.FontDescription = Style.FontDescription;
			layout.Ellipsize = Pango.EllipsizeMode.End;
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (FontSize);
			layout.FontDescription.Weight = Pango.Weight.Bold;
			
			Pango.Rectangle logical, ink;
			layout.GetPixelExtents (out ink, out logical);
			
			HasTooltip = logical.Width > MaxWidth;
			TextWidth = Math.Min (MaxWidth, Math.Max (MinWidth, logical.Width)) + 34;
			SetSizeRequest (TextWidth, 22);
		}

		void ItemDisabledChanged (object sender, EventArgs e)
		{
			QueueDraw ();
		}

		void ItemTextChanged (object sender, EventArgs e)
		{
			CalcTextWidth ();
			QueueDraw ();
		}

		void ItemIconChanged (object sender, EventArgs e)
		{
			QueueDraw ();
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (!item.Disabled)
				item.SendClick ();
			return item.Disabled;
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			if (!item.Disabled) {
				hovered = true;
				QueueDraw ();
			}
			return false;
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (!item.Disabled) {
				hovered = false;
				QueueDraw ();
			}
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnQueryTooltip (int x, int y, bool keyboard_tooltip, Tooltip tooltip)
		{
			tooltip.Text = item.Text;
			return true;
		}
		
		void PlacePixbuf (Context cr, Pixbuf pixbuf, Gdk.Rectangle allocation)
		{
			int iconSize = allocation.Height - IconBuffer * 2;
			
			int x = allocation.X + 1 + ((iconSize - pixbuf.Width) / 2);
			int y = allocation.Y + IconBuffer + ((iconSize - pixbuf.Height) / 2);
			
			Gdk.CairoHelper.SetSourcePixbuf (cr, pixbuf, x, y);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return false;
			
			Gdk.Rectangle allocation = Allocation;
			
			Gdk.Pixbuf pixbuf = DockServices.Drawing.LoadIcon (item.Icon, allocation.Height - IconBuffer * 2);
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				if (hovered && !item.Disabled) {
					cr.Rectangle (allocation.X, allocation.Y, allocation.Width, allocation.Height);
					cr.Color = TextColor.SetAlpha (.1);
					cr.Fill ();
				}

				PlacePixbuf (cr, pixbuf, allocation);
				cr.PaintWithAlpha (item.Disabled ? 0.5 : 1);
				
				if (item.Bold) {
					cr.Operator = Operator.Add;
					PlacePixbuf (cr, pixbuf, allocation);
					cr.PaintWithAlpha (.8);
					cr.Operator = Operator.Over;
				}
				
				if (!string.IsNullOrEmpty (item.Emblem)) {
					Gdk.Pixbuf emblem = DockServices.Drawing.LoadIcon (item.Emblem, allocation.Height - IconBuffer * 2);
					PlacePixbuf (cr, pixbuf, allocation);
					cr.Paint ();
					emblem.Dispose ();
				}
			
				Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
				layout.SetText (item.Text);
				layout.Width = Pango.Units.FromPixels (allocation.Width - allocation.Height - 10);
				layout.FontDescription = Style.FontDescription;
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (FontSize);
				layout.FontDescription.Weight = Pango.Weight.Bold;
				
				Pango.Rectangle logical, ink;
				layout.GetPixelExtents (out ink, out logical);
				
				cr.MoveTo (allocation.X + allocation.Height + 5, allocation.Y + (allocation.Height - logical.Height) / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Color = TextColor.SetAlpha (item.Disabled ? 0.5 : 1);
				cr.Fill ();
			}
			
			pixbuf.Dispose ();
			return true;
		}
	}
}
