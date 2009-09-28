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
		
			BuildItems ();
			
			FileSystemWatcher watcher = new FileSystemWatcher (Path.GetDirectoryName (BookmarksFile));
			watcher.Filter = ".gtk-bookmarks";
			watcher.IncludeSubdirectories = false;
			watcher.Renamed += WatcherRenamed;
			
			watcher.EnableRaisingEvents = true;
		}

		void WatcherRenamed (object sender, RenamedEventArgs e)
		{
			Gtk.Application.Invoke (delegate {
				UpdateItems ();
			});
		}
		
		void BuildItems ()
		{
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
					if (item != null) {
						item.Owner = this;
						items.Add (item);
					}
				}
				
				reader.Dispose ();
			}
		}
		
		void UpdateItems ()
		{
			List<AbstractDockItem> old = items;
			items = new List<AbstractDockItem> ();
			
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
					
					if (old.Cast<FileDockItem> ().Any (fdi => fdi.Uri == uri)) {
						FileDockItem item = old.Cast<FileDockItem> ().Where (fdi => fdi.Uri == uri).First ();
						old.Remove (item);
						items.Add (item);
					} else {
						FileDockItem item = FileDockItem.NewFromUri (uri);
						if (item != null) {
							item.Owner = this;
							items.Add (item);
						}
					}
				}
				
				
				reader.Dispose ();
			}
			
			foreach (AbstractDockItem item in old)
				item.Dispose ();
			
			OnItemsChanged (items, old);
		}

		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "Bookmark Items";
			}
		}
		
		public override bool Separated {
			get {
				return true;
			}
		}
		
		public override IEnumerable<AbstractDockItem> Items {
			get {
				return items.Cast<AbstractDockItem> ();
			}
		}
		
		public override void Dispose ()
		{
			foreach (FileDockItem item in items)
				item.Dispose ();
		}
		
		#endregion
	}
}
