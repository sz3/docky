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
using System.Linq;
using System.Collections.Generic;

using GLib;
using Gtk;
using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace RecentDocuments
{
	public class RecentDocumentsItemProvider : AbstractDockItemProvider
	{
		#region AbstractDockItemProvider implementation
		public override string Name {
			get {
				return "Recent Documents";
			}
		}
		
		public override string Icon {
			get {
				return "folder-recent;;document-open-recent";
			}
		}
		
		#endregion
		
		RecentDocumentsItem docs;
		
		public RecentDocumentsItemProvider ()
		{
			docs = new RecentDocumentsItem ();
			Items = docs.AsSingle<AbstractDockItem> ();
		}
		
		void ClearRecent ()
		{
			Gtk.MessageDialog md = new Gtk.MessageDialog (null, 
					  0,
					  Gtk.MessageType.Warning, 
					  Gtk.ButtonsType.None,
					  "<b><big>" + Catalog.GetString ("Clear the Recent Documents list?") + "</big></b>");
			
			md.Title = Catalog.GetString ("Clear Recent Documents");
			md.Icon = DockServices.Drawing.LoadIcon ("docky", 22);
			md.SecondaryText = Catalog.GetString ("If you clear the Recent Documents list, you clear the following:\n" +
				"\u2022 All items from the Places \u2192 Recent Documents menu item.\n" +
				"\u2022 All items from the recent documents list in all applications.");
			md.Modal = false;
			
			md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			md.AddButton (Gtk.Stock.Clear, Gtk.ResponseType.Ok);
			md.DefaultResponse = Gtk.ResponseType.Ok;

			md.Response += (o, args) => {
				if (args.ResponseId != Gtk.ResponseType.Cancel)
					Gtk.RecentManager.Default.PurgeItems ();
				md.Destroy ();
			};
			
			md.Show ();
		}
		
		public override MenuList GetMenuItems (AbstractDockItem item)
		{
			MenuList list = base.GetMenuItems (item);
			list[MenuListContainer.Footer].Add (new Docky.Menus.MenuItem (Catalog.GetString ("_Clear Recent Documents..."), "edit-clear", (o, a) => ClearRecent (), docs.RecentDocs.Count == 0));
			return list;
		}
	}
}
