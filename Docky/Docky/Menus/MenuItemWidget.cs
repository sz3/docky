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
using Docky.CairoHelper;

namespace Docky.Menus
{
	internal class MenuItemWidget : Gtk.EventBox
	{
		const int MenuHeight = 22;
		const int MinWidth = 100;
		const int MaxWidth = 350;
		const int FontSize = 11;
		const int Padding = 4;
		const int IconBuffer = Padding - 1;
		
		public MenuItem item;
		
		public event EventHandler SelectedChanged;
		
 		public bool Selected { get; set; }
		
		public bool MenuShowingIcons { get; set; }
		
		public Cairo.Color TextColor { get; set; }
		
		public int TextWidth { get; protected set; }
				
		DockySurface icon_surface, emblem_surface;
		
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
			char accel;
			Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
			layout.SetMarkupWithAccel (item.Text, '_', out accel);
			layout.Width = Pango.Units.FromPixels (2 * MaxWidth);
			layout.FontDescription = Style.FontDescription;
			layout.Ellipsize = Pango.EllipsizeMode.End;
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (FontSize);
			layout.FontDescription.Weight = Pango.Weight.Bold;
			
			Pango.Rectangle logical, ink;
			layout.GetPixelExtents (out ink, out logical);
			
			HasTooltip = logical.Width > MaxWidth;
			TextWidth = Math.Min (MaxWidth, Math.Max (MinWidth, logical.Width)) + 2 * Padding + 1;
			if (item.ShowIcons)
				TextWidth += MenuHeight + Padding;
			SetSizeRequest (TextWidth, MenuHeight);
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
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (!item.Disabled && !Selected) {
				Selected = true;
				if (SelectedChanged != null)
					SelectedChanged (this, EventArgs.Empty);
				QueueDraw ();
			}
			return false;
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			if (!item.Disabled && !Selected) {
				Selected = true;
				if (SelectedChanged != null)
					SelectedChanged (this, EventArgs.Empty);
				QueueDraw ();
			}
			return false;
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (!item.Disabled && Selected) {
				Selected = false;
				if (SelectedChanged != null)
					SelectedChanged (this, EventArgs.Empty);
				QueueDraw ();
			}
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnQueryTooltip (int x, int y, bool keyboard_tooltip, Tooltip tooltip)
		{
			tooltip.Text = item.Text;
			return true;
		}
		
		void PlaceSurface (Context cr, DockySurface surface, Gdk.Rectangle allocation)
		{
			int iconSize = allocation.Height - IconBuffer * 2;
			
			int x = allocation.X + Padding + ((iconSize - surface.Width) / 2);
			int y = allocation.Y + IconBuffer + ((iconSize - surface.Height) / 2);
			
			cr.SetSource (surface.Internal, x, y);
		}
		
		DockySurface LoadIcon (string icon, int size)
		{
			bool monochrome = icon.StartsWith ("[monochrome]");
			if (monochrome) {
				icon = icon.Substring ("[monochrome]".Length);
			}
			
			DockySurface surface;
			using (Gdk.Pixbuf pixbuf = DockServices.Drawing.LoadIcon (icon, size)) {
				surface = new DockySurface (pixbuf.Width, pixbuf.Height);
				Gdk.CairoHelper.SetSourcePixbuf (surface.Context, pixbuf, 0, 0);
				surface.Context.Paint ();
			}
			
			if (monochrome) {
				surface.Context.Operator = Operator.Atop;
				double v = TextColor.GetValue ();
				// reduce value by 20%
				surface.Context.Color = TextColor.SetValue (v * .8);
				surface.Context.Paint ();
				surface.ResetContext ();
			}
			
			return surface;
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return false;
			
			Gdk.Rectangle allocation = Allocation;
			
			int pixbufSize = allocation.Height - IconBuffer * 2;
			if (item.ShowIcons && (icon_surface == null || (icon_surface.Height != pixbufSize && icon_surface.Width != pixbufSize))) {
				if (icon_surface != null)
					icon_surface.Dispose ();
				if (emblem_surface != null)
					emblem_surface.Dispose ();
				
				icon_surface = LoadIcon (item.Icon, pixbufSize);
				
				if (!string.IsNullOrEmpty (item.Emblem))
					emblem_surface = LoadIcon (item.Emblem, pixbufSize);
			}
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				if (Selected && !item.Disabled) {
					cr.Rectangle (allocation.X, allocation.Y, allocation.Width, allocation.Height);
					cr.Color = TextColor.SetAlpha (.1);
					cr.Fill ();
				}
				
				if (item.ShowIcons) {
					PlaceSurface (cr, icon_surface, allocation);
					cr.PaintWithAlpha (item.Disabled ? 0.5 : 1);
				}
				
				if (item.Bold) {
					cr.Operator = Operator.Add;
					PlaceSurface (cr, icon_surface, allocation);
					cr.PaintWithAlpha (.8);
					cr.Operator = Operator.Over;
				}
				
				if (item.ShowIcons && !string.IsNullOrEmpty (item.Emblem)) {
					PlaceSurface (cr, emblem_surface, allocation);
					cr.Paint ();
				}
			
				Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ();
				char accel;
				layout.SetMarkupWithAccel (item.Text, '_', out accel);
				if (item.ShowIcons)
					layout.Width = Pango.Units.FromPixels (allocation.Width - allocation.Height - 3 * Padding - 1);
				else
					layout.Width = Pango.Units.FromPixels (allocation.Width - 2 * Padding - 1);
				layout.FontDescription = Style.FontDescription;
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (FontSize);
				layout.FontDescription.Weight = Pango.Weight.Bold;
				
				Pango.Rectangle logical, ink;
				layout.GetPixelExtents (out ink, out logical);
				
				int offset;
				if (item.ShowIcons || MenuShowingIcons)
					offset = allocation.Height + 2 * Padding;
				else
					offset = Padding;
				cr.MoveTo (allocation.X + offset, allocation.Y + (allocation.Height - logical.Height) / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Color = TextColor.SetAlpha (item.Disabled ? 0.5 : 1);
				cr.Fill ();
			}
			
			return true;
		}
		
		public override void Dispose ()
		{
			if (icon_surface != null)
				icon_surface.Dispose ();
			
			if (emblem_surface != null)
				emblem_surface.Dispose ();
			base.Dispose ();
		}
	}
}
