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
using System.IO;

using Docky.Items;
using Docky.Menus;

namespace Bookmarks
{
	public class BookmarkDockItem : FileDockItem
	{
		public static new BookmarkDockItem NewFromUri (string uri)
		{
			string path = Gnome.Vfs.Global.GetLocalPathFromUri (uri);
			if (!Directory.Exists (path) && !File.Exists (path)) {
				return null;
			}
			
			return new BookmarkDockItem (uri);
		}
		
		BookmarkDockItem (string uri) : base (uri)
		{
		}
		
		void Remove ()
		{
			(Owner as BookmarksItemProvider).RemoveBookmark (this);
		}

		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			foreach (MenuItem item in base.GetMenuItems())
				yield return item;
			
			yield return new MenuItem ("Remove", "gtk-remove", (o, a) => Remove());
		}
	}
}
