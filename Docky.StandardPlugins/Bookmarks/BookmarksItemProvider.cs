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
using System.Linq;
using System.IO;

using Docky.Items;

namespace Bookmarks
{
	public class BookmarksItemProvider : AbstractDockItemProvider
	{
		List<AbstractDockItem> items;
		
		string BookmarksFile {
			get {
				return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".gtk-bookmarks");
			}
		}
		
		public BookmarksItemProvider ()
		{
			items = new List<AbstractDockItem> ();
		
			UpdateItems ();
			
			FileSystemWatcher watcher = new FileSystemWatcher (Path.GetDirectoryName (BookmarksFile));
			watcher.Filter = ".gtk-bookmarks";
			watcher.IncludeSubdirectories = false;
			watcher.Renamed += WatcherRenamed;
			watcher.Changed += WatcherChanged;
			
			watcher.EnableRaisingEvents = true;
		}

		void WatcherChanged (object sender, FileSystemEventArgs e)
		{
			Gtk.Application.Invoke (delegate {
				UpdateItems ();
			});
		}

		void WatcherRenamed (object sender, RenamedEventArgs e)
		{
			Gtk.Application.Invoke (delegate {
				UpdateItems ();
			});
		}
		
		void UpdateItems ()
		{
			List<AbstractDockItem> old = items;
			items = new List<AbstractDockItem> ();
			
			if (File.Exists (BookmarksFile)) {
				using (StreamReader reader = new StreamReader (BookmarksFile)) {
					while (!reader.EndOfStream) {
						string uri = reader.ReadLine ().Split (' ').First ();
						
						if (old.Cast<BookmarkDockItem> ().Any (fdi => fdi.Uri == uri)) {
							BookmarkDockItem item = old.Cast<BookmarkDockItem> ().Where (fdi => fdi.Uri == uri).First ();
							old.Remove (item);
							items.Add (item);
						} else {
							BookmarkDockItem item = BookmarkDockItem.NewFromUri (uri);
							if (item != null) {
								item.Owner = this;
								items.Add (item);
							}
						}
					}
				}
			}
			
			foreach (AbstractDockItem item in old)
				item.Dispose ();
			
			OnItemsChanged (items, old);
		}
		
		public void RemoveBookmark (BookmarkDockItem item)
		{
		}

		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "Bookmark Items";
			}
		}
		
		public override bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return items.Contains (item);
		}
		
		public override bool RemoveItem (AbstractDockItem item)
		{
			if (!ItemCanBeRemoved (item))
				return false;
			
			BookmarkDockItem bookmark = item as BookmarkDockItem;
			
			if (File.Exists (BookmarksFile)) {
				string tempPath = Path.GetTempFileName();
				
				using (StreamReader reader = new StreamReader (BookmarksFile)) {
					using (StreamWriter writer = new StreamWriter(File.OpenWrite(tempPath))) {
						while (!reader.EndOfStream) {
							string line = reader.ReadLine();
							if (bookmark.Uri != line)
								writer.WriteLine(line);
						}
					}
				}
				
				if (File.Exists (tempPath)) {
					File.Delete (BookmarksFile);
					File.Move (tempPath, BookmarksFile);
				}
			}
			
			UpdateItems ();
			return true;
		}
		
		public override bool Separated {
			get {
				return true;
			}
		}
		
		public override IEnumerable<AbstractDockItem> Items {
			get {
				return items;
			}
		}
		
		public override void Dispose ()
		{
			foreach (AbstractDockItem item in items)
				item.Dispose ();
		}
		
		#endregion
	}
}
