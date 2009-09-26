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
using System.IO;
using System.Text;

using Docky.Items;

namespace Bookmarks
{


	public class BookmarksItemProvider : IDockItemProvider
	{
		List<FileDockItem> items;
		
		string BookmarksFile {
			get {
				return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".gtk-bookmarks");
			}
		}
		
		public BookmarksItemProvider ()
		{
			items = new List<FileDockItem> ();
		
			if (File.Exists (BookmarksFile)) {
				StreamReader reader;
				try {
					reader = new StreamReader (BookmarksFile);
				} catch {
					return;
				}
			
				string uri;
				while (!reader.EndOfStream) {
					uri = reader.ReadLine ().Split (' ').First ();
					FileDockItem item = FileDockItem.NewFromUri (uri);
					if (item != null)
						items.Add (item);
				}
				
				reader.Dispose ();
			}
		}

		#region IDockItemProvider implementation
		public event EventHandler<ItemsChangedArgs> ItemsChanged;
		
		public string Name {
			get {
				return "Bookmark Items";
			}
		}
		
		public bool Separated {
			get {
				return true;
			}
		}
		
		public IEnumerable<AbstractDockItem> Items {
			get {
				return items.Cast<AbstractDockItem> ();
			}
		}

		public bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return false;
		}
		
		public bool RemoveItem (AbstractDockItem item)
		{
			return false;
		}
		#endregion
		
		#region IDisposable implementation
		public void Dispose ()
		{
			foreach (FileDockItem item in items)
				item.Dispose ();
		}
		#endregion

	}
}
