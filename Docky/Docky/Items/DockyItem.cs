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
using Mono.Unix;

using Docky.Menus;

namespace Docky.Items
{
	internal class DockyItem : ColoredIconDockItem, INonPersistedItem
	{
		public DockyItem ()
		{
			Indicator = ActivityIndicator.Single;
			HoverText = "Docky";
			Icon = "docky";
		}
		
		protected override void OnStyleSet (Gtk.Style style)
		{
			Gdk.Color gdkColor = Style.Backgrounds [(int) Gtk.StateType.Selected];
			int hue = (int) new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											1.0).GetHue ();
			HueShift = (((hue - 202) % 360) + 360) % 360;
		}
		
		public override string UniqueID ()
		{
			return "DockyItem";
		}
		
		protected override void OnScrolled (ScrollDirection direction, ModifierType mod)
		{
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				Docky.Config.Show ();
				return ClickAnimation.Bounce;
			}
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			// intentionally dont inherit
			MenuList list = new MenuList ();
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Settings"), "gtk-preferences", (o, a) => Docky.Config.Show ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_About"), "gtk-about", (o, a) => Docky.ShowAbout ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Quit Docky"), "gtk-quit", (o, a) => Gtk.Application.Quit ()));
			return list;
		}

	}
}
