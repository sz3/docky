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

using Docky.Items;
using Docky.Menus;

namespace Bookmarks
{
	public class BookmarkDockItem : FileDockItem
	{
		public static new BookmarkDockItem NewFromUri (string uri, string name)
		{	
			return new BookmarkDockItem (uri, name);
		}
		
		BookmarkDockItem (string uri, string name) : base (uri)
		{
			// incase the icon is null, give it a generic folder icon
			// this can happen with a bookmark that's not native,
			// or doesn't have a path for some reason
			if (string.IsNullOrEmpty (this.Icon))
				Icon = "folder";
			
			if (string.IsNullOrEmpty (name))
				HoverText = OwnedFile.Basename;
			else 
				HoverText = name;
		}
		
		void Remove ()
		{
			Owner.RemoveItem (this);
		}

		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			/*
			foreach (MenuItem item in base.GetMenuItems())
				yield return item;
			*/
			yield return new MenuItem ("Remove", "gtk-remove", (o, a) => Remove());
		}
	}
}