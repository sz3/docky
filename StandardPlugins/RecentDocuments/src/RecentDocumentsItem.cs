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
		List<File> RecentDocs;
		File CurrentFile;
		int CurrentIndex = 0;
		
		public RecentDocumentsItem ()
		{
			RecentDocs = new List<File> ();
			
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
				                     .Select (f => FileFactory.NewForUri (f.Uri)));
			}
		}
		
		void UpdateInfo ()
		{
			CurrentFile = RecentDocs.ElementAt (CurrentIndex);
			
			SetIconFromGIcon (CurrentFile.Icon ());
	
			HoverText = CurrentFile.Basename;
			
			OnPaintNeeded ();
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			CurrentIndex += NumRecentDocs;
			
			if (direction == Gdk.ScrollDirection.Up)
				CurrentIndex += 1;
			else if (direction == Gdk.ScrollDirection.Down)
				CurrentIndex -= 1;
			
			CurrentIndex %= NumRecentDocs;
			
			UpdateInfo ();
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				DockServices.System.Open (CurrentFile);
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}

		public override MenuList GetMenuItems ()
		{
			MenuList list = base.GetMenuItems ();
			
			foreach (File _f in RecentDocs) {
				GLib.File f = _f;
				string icon = DockServices.Drawing.IconFromGIcon (f.Icon ());
				MenuItem item = new MenuItem (f.Basename, icon, (o, a) => DockServices.System.Open (f));
				list[MenuListContainer.RelatedItems].Add (item);
			}
			
			return list;
		}
	}
}
