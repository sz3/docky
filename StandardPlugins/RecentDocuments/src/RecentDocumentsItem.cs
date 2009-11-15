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
		List<FileDockItem> RecentDocs;
		FileDockItem CurrentFile;
		int CurrentIndex = 0;
		
		public RecentDocumentsItem ()
		{
			RecentDocs = new List<FileDockItem> ();
			
			Gtk.RecentManager.Default.Changed += (o, e) => RefreshRecentDocs ();
			
			RefreshRecentDocs ();
			UpdateInfo ();
		}
		
		void RefreshRecentDocs ()
		{
			GLib.List recent_items = new GLib.List (Gtk.RecentManager.Default.Items.Handle, typeof(Gtk.RecentInfo));
			
			lock (RecentDocs) {
				RecentDocs.Clear ();
				
				RecentDocs.AddRange (recent_items.Cast<Gtk.RecentInfo> ()
				                     .Where (it => it.Exists ())
				                     .Take (NumRecentDocs)
				                     .Select (f => FileDockItem.NewFromUri (f.Uri)));
			}
		}
		
		void UpdateInfo ()
		{
			CurrentFile = RecentDocs.ElementAt (CurrentIndex);
			
			Icon = CurrentFile.Icon;
	
			HoverText = CurrentFile.HoverText;
			
			OnPaintNeeded ();
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			CurrentIndex += NumRecentDocs;
			
			if (direction == Gdk.ScrollDirection.Up)
				CurrentIndex -= 1;
			else if (direction == Gdk.ScrollDirection.Down)
				CurrentIndex += 1;
			
			CurrentIndex %= NumRecentDocs;
			
			UpdateInfo ();
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				DockServices.System.Open (CurrentFile.OwnedFile);
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}

		public override MenuList GetMenuItems ()
		{
			MenuList list = base.GetMenuItems ();
			
			foreach (FileDockItem _f in RecentDocs) {
				FileDockItem f = _f;
				if (!f.OwnedFile.Exists)
					continue;

				MenuItem item = new MenuItem (f.OwnedFile.Basename, f.Icon, (o, a) => DockServices.System.Open (f.OwnedFile));
				list[MenuListContainer.RelatedItems].Add (item);
			}
			
			// check to make sure our right click menu has the same number of items as RecentDocs
			// if it doesn't, one of the recent docs might have been deleted, so update the list.
			if (list[MenuListContainer.RelatedItems].Count () != RecentDocs.Count ()) {
				RefreshRecentDocs ();
				if (RecentDocs.Any (f => f.OwnedFile.Path == CurrentFile.OwnedFile.Path))
					CurrentIndex = RecentDocs.IndexOf (RecentDocs.First (f => f.OwnedFile.Path == CurrentFile.OwnedFile.Path));
				UpdateInfo ();
			}
			
			return list;
		}
	}
}
