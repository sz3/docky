//  
//  Copyright (C) 2009 Robert Dyer
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

using GLib;
using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Bookmarks
{
	public class BookmarkDockItem : FileDockItem
	{
		public BookmarkDockItem (string uri, string name) : base(uri)
		{
			// incase the icon is null, give it a generic folder icon
			// this can happen with a bookmark that's not mounted,
			// or doesn't have a path for some reason
			if (string.IsNullOrEmpty (this.Icon))
				Icon = "folder";
			
			if (string.IsNullOrEmpty (name))
				HoverText = OwnedFile.Basename;
			else
				HoverText = name;
			
			OwnedFile.AddMountAction (() => {
				SetIconFromGIcon (OwnedFile.Icon ());
				OnPaintNeeded ();
			});
		}
		
		void Remove ()
		{
			Owner.RemoveItem (this);
		}

		protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			return true;
		}

		protected override bool OnAcceptDrop (IEnumerable<string> uris)
		{
			bool retVal = false;
			OwnedFile.MountWithActionAndFallback (() => {
				retVal = base.OnAcceptDrop (uris);
			}, () => {
				retVal = base.OnAcceptDrop (uris);
			});
			return retVal;
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				Open ();
				return ClickAnimation.Bounce;
			}
			return ClickAnimation.None;
		}

		protected override MenuList OnGetMenuItems ()
		{
			// intentionally dont inherit
			MenuList list = new MenuList ();
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Open"), "gtk-open", (o, a) => Open ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Remove"), "gtk-remove", (o, a) => Remove ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Reset _Color"), "edit-clear", (o, a) => ResetHue (), HueShift == 0));
			return list;
		}
	}
}
