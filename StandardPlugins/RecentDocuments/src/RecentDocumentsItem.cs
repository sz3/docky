//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
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
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;

using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace RecentDocuments
{
	public class RecentDocumentsItem : IconDockItem
	{
		#region AbstractDockItem implementation
		
		public override string UniqueID ()
		{
			return "RecentDocuments";
		}
		
		#endregion
		
		const int NumRecentDocs = 7;
		
		internal List<FileDockItem> RecentDocs;
		
		FileDockItem currentFile;
		FileMonitor watcher;
		
		public FileDockItem CurrentFile {
			get {
				return currentFile;
			}
			set {
				if (currentFile == value)
					return;
				currentFile = value;
				
				if (watcher != null) {
					watcher.Changed -= WatcherChanged;
					watcher = null;
				}
				
				if (value != null) {
					watcher = FileMonitor.File (currentFile.OwnedFile, FileMonitorFlags.None, null);
					watcher.Changed += WatcherChanged;
				}
			}
		}
		
		void WatcherChanged (object o, ChangedArgs args)
		{
			RefreshRecentDocs ();
		}
		
		public RecentDocumentsItem ()
		{
			RecentDocs = new List<FileDockItem> ();
			
			Gtk.RecentManager.Default.Changed += delegate { RefreshRecentDocs (); };
			
			CurrentFile = null;
			RefreshRecentDocs ();
		}
		
		void RefreshRecentDocs ()
		{
			GLib.List recent_items = new GLib.List (Gtk.RecentManager.Default.Items.Handle, typeof(Gtk.RecentInfo));
			
			lock (RecentDocs) {
				RecentDocs.Clear ();
				
				RecentDocs.AddRange (recent_items.Cast<Gtk.RecentInfo> ()
				                     .Where (it => it.Exists ())
									 .OrderByDescending (f => f.Modified)
				                     .Take (NumRecentDocs)
				                     .Select (f => FileDockItem.NewFromUri (f.Uri)));
			}
			
			UpdateInfo ();
		}
		
		void UpdateInfo ()
		{
			if (RecentDocs.Count() == 0)
				CurrentFile = null;
			
			if (CurrentFile != null && RecentDocs.IndexOf (CurrentFile) == -1)
				CurrentFile = RecentDocs.First ();
			
			if (CurrentFile == null) {
				Icon = "folder-recent;;document-open-recent";
				HoverText = "Recent Documents";
			} else {
				Icon = CurrentFile.Icon;
				HoverText = CurrentFile.HoverText;
			}
			QueueRedraw ();
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			int offset = Math.Min (NumRecentDocs, RecentDocs.Count ());
			int currentIndex = RecentDocs.IndexOf (CurrentFile);
			
			currentIndex += offset;
			
			if (direction == Gdk.ScrollDirection.Up)
				currentIndex -= 1;
			else if (direction == Gdk.ScrollDirection.Down)
				currentIndex += 1;
			
			if (offset == 0)
				currentIndex = 0;
			else
				currentIndex %= offset;
			
			try {
				CurrentFile = RecentDocs.ElementAt (currentIndex);
			} catch (Exception) {
				CurrentFile = null;
			}
			UpdateInfo ();
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1 && CurrentFile != null) {
				DockServices.System.Open (CurrentFile.OwnedFile);
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}

		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			foreach (FileDockItem _f in RecentDocs) {
				FileDockItem f = _f;
				if (!f.OwnedFile.Exists)
					continue;

				MenuItem item = new IconMenuItem (f.OwnedFile.Basename, f.Icon, (o, a) => DockServices.System.Open (f.OwnedFile));
				item.Mnemonic = null;
				list[MenuListContainer.RelatedItems].Add (item);
			}
			
			// check to make sure our right click menu has the same number of items as RecentDocs
			// if it doesn't, one of the recent docs might have been deleted, so update the list.
			if (list[MenuListContainer.RelatedItems].Count () != RecentDocs.Count ())
				RefreshRecentDocs ();
			
			return list;
		}

		public override void Dispose ()
		{
			if (watcher != null) {
				watcher.Changed -= WatcherChanged;
				watcher.Dispose ();
			}
			base.Dispose ();
		}
	}
}
