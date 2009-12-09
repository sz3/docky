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
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Gdk;
using Gtk;
using Cairo;
using Mono.Unix;

using Docky.Menus;
using Docky.Services;
using Docky.CairoHelper;

namespace Docky.Items
{
	public abstract class ColoredIconDockItem : IconDockItem
	{
		static IPreferences prefs = DockServices.Preferences.Get <ColoredIconDockItem> ();
		static Regex hueRegex = new Regex ("[^a-zA-Z0-9]");
		
		int? shift;
		public int HueShift {
			get {
				if (!shift.HasValue)
					HueShift = prefs.Get<int> (hueRegex.Replace (UniqueID (), "_"), 0);
				return shift.Value;
			}
			protected set {
				if (shift.HasValue && shift.Value == value)
					return;
				shift = value;
				prefs.Set<int> (hueRegex.Replace (UniqueID (), "_"), value);
				
				QueueRedraw ();
			}
		}
		
		public ColoredIconDockItem ()
		{
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{			
			Gdk.Pixbuf pbuf;
			
			if (ForcePixbuf == null)
				pbuf = DockServices.Drawing.LoadIcon (Icon, surface.Width, surface.Height);
			else
				pbuf = DockServices.Drawing.ARScale (surface.Width, surface.Height, ForcePixbuf);
			
			if (HueShift != 0)
				pbuf = DockServices.Drawing.AddHueShift (pbuf, HueShift);

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
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			list[MenuListContainer.Footer].Add (new Menus.MenuItem (Catalog.GetString ("Reset Color"), "edit-clear", (o, a) => ResetHue (), HueShift == 0));
			return list;
		}
		
		protected override void OnScrolled (ScrollDirection direction, ModifierType mod)
		{
			int shift = HueShift;
			
			if (direction == Gdk.ScrollDirection.Up)
				shift += 5;
			else if (direction == Gdk.ScrollDirection.Down)
				shift -= 5;
			
			if (shift < 0)
				shift += 360;
			shift %= 360;
			
			HueShift = shift;
		}
		
		protected void ResetHue ()
		{
			HueShift = 0;
		}
	}
}
