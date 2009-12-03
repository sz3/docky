//  
//  Copyright (C) 2009 Jason Smith, Chris Szikszoy, Robert Dyer
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
using System.Text;

using Mono.Unix;

using GConf;
using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Trash
{
	public class TrashDockItem : FileDockItem
	{
		uint ItemsInTrash {
			get {
				FileInfo info = OwnedFile.QueryInfo ("trash::item-count", FileQueryInfoFlags.None, null);
				return info.GetAttributeUInt ("trash::item-count");
			}
		}
		
		bool TrashFull {
			get {
				return ItemsInTrash > 0;
			}
		}
		
		FileMonitor TrashMonitor { get; set; }
		
		public TrashDockItem () : base ("trash://")
		{
			Update ();
		
			TrashMonitor = OwnedFile.Monitor (FileMonitorFlags.None, null);
			TrashMonitor.Changed += HandleChanged;
		}

		void HandleChanged (object o, ChangedArgs args)
		{			
			Gtk.Application.Invoke (delegate {
				Update ();
			});
		}

		void Update ()
		{
			// this can be a little costly, let's just call it once and store locally
			uint itemsInTrash = ItemsInTrash;
			if (itemsInTrash == 0)
				HoverText = Catalog.GetString ("No items in Trash");
			else
				HoverText = string.Format (Catalog.GetPluralString ("{0} item in Trash", "{0} items in Trash", (int) itemsInTrash), itemsInTrash);
			
			SetIconFromGIcon (OwnedFile.Icon ());
		}
		
		public override string UniqueID ()
		{
			return "TrashCan";
		}
		
		protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			bool accepted = false;
			
			foreach (string uri in uris)
				accepted |= CanReceiveItem (uri);

			return accepted;
		}
		
		protected override bool OnCanAcceptDrop (AbstractDockItem item)
		{
			if (item == this)
				return false;

			if (item.Owner == null)
				return false;

			return item.Owner.ItemCanBeRemoved (item);
		}
		
		protected override bool OnAcceptDrop (AbstractDockItem item)
		{
			if (!CanAcceptDrop (item))
				return false;

			item.Owner.RemoveItem (item);
			return true;
		}
		
		protected override bool OnAcceptDrop (IEnumerable<string> uris)
		{
			bool accepted = false;
			
			foreach (string uri in uris)
				accepted |= ReceiveItem (uri);

			return accepted;
		}
		
		bool CanReceiveItem (string uri)
		{
			// if the file doesn't exist for whatever reason, we bail
			return FileFactory.NewForUri (uri).Exists;
		}
		
		bool ReceiveItem (string uri)
		{
			bool trashed = FileFactory.NewForUri (uri).Trash (null);
			
			if (trashed) {
				Update ();
				OnPaintNeeded ();
			}
			else
				Log<TrashDockItem>.Error ("Could not move {0} to trash.'", uri);
			
			return trashed;
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				OpenTrash ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			// intentionally dont inherit
			MenuList list = new MenuList ();
			list[MenuListContainer.Actions].Add (
				new MenuItem (Catalog.GetString ("_Open Trash"), Icon, (o, a) => OpenTrash ()));
			list[MenuListContainer.Actions].Add (
				new MenuItem (Catalog.GetString ("_Empty Trash"), "gtk-clear", (o, a) => EmptyTrash (), !TrashFull));
			return list;
		}
		
		void OpenTrash ()
		{
			DockServices.System.Open (OwnedFile);
		}
		
		void EmptyTrash ()
		{
			bool confirm = true;
			
			try {
				confirm = (bool) new Client ().Get ("/apps/nautilus/preferences/confirm_trash");
			} catch {}
			
			if (confirm) {
				Gtk.MessageDialog md = new Gtk.MessageDialog (null, 
						  0,
						  Gtk.MessageType.Warning, 
						  Gtk.ButtonsType.None,
						  "<b><big>" + Catalog.GetString ("Empty all of the items from the trash?") + "</big></b>");
				md.Icon = DockServices.Drawing.LoadIcon ("docky", 22);
				md.SecondaryText = Catalog.GetString ("If you choose to empty the trash, all items in it\n" +
					"will be permanently lost. Please note that you\n" +
					"can also delete them separately.");
				md.Modal = false;
				
				Gtk.Button cancel_button = new Gtk.Button();
				cancel_button.CanFocus = true;
				cancel_button.Name = "cancel_button";
				cancel_button.UseStock = true;
				cancel_button.UseUnderline = true;
				cancel_button.Label = "gtk-cancel";
				cancel_button.Show ();
				md.AddActionWidget (cancel_button, Gtk.ResponseType.Cancel);
				md.AddButton (Catalog.GetString ("Empty _Trash"), Gtk.ResponseType.Ok);
				md.DefaultResponse = Gtk.ResponseType.Ok;

				md.Response += (o, args) => {
					if (args.ResponseId != Gtk.ResponseType.Cancel)
						PerformEmptyTrash ();
					md.Destroy ();
				};
				
				md.Show ();
			} else {
				PerformEmptyTrash ();
			}
		}
		
		void PerformEmptyTrash ()
		{
			// disable events for a minute
			TrashMonitor.Changed -= HandleChanged;
			
			DockServices.System.RunOnMainThread (() => {
				OwnedFile.Delete_Recurse ();
			});
			
			// eneble events again
			TrashMonitor.Changed += HandleChanged;

			Gtk.Application.Invoke (delegate {
				Update ();
				OnPaintNeeded ();
			});
		}
	}
}
