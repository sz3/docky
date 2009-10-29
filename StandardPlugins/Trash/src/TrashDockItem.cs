//  
//  Copyright (C) 2009 Jason Smith, Chris Szikszoy
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

using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Trash
{


	public class TrashDockItem : FileDockItem
	{
		const string OneInTrash = "1 item in Trash";
		const string ManyInTrash = "{0} items in Trash";
		
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
			switch (itemsInTrash) {
			case 0:
				HoverText = string.Format (ManyInTrash, "No");
				break;
			case 1:
				HoverText = string.Format (OneInTrash, "1");
				break;
			default:
				HoverText = string.Format (ManyInTrash, itemsInTrash);
				break;
			}
			
			SetIconFromGIcon (OwnedFile.Icon ());
		}
		
		public override string UniqueID ()
		{
			return "TrashCan";
		}
		
		public override bool CanAcceptDrop (IEnumerable<string> uris)
		{
			bool accepted = false;
			
			foreach (string uri in uris)
				accepted |= CanReceiveItem (uri);

			return accepted;
		}
		
		public override bool CanAcceptDrop (AbstractDockItem item)
		{
			if (item == this)
				return false;

			if (item.Owner == null)
				return false;

			return item.Owner.ItemCanBeRemoved (item);
		}
		
		public override bool AcceptDrop (AbstractDockItem item)
		{
			if (!CanAcceptDrop (item))
				return false;

			item.Owner.RemoveItem (item);
			return true;
		}
		
		public override bool AcceptDrop (IEnumerable<string> uris)
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
		
		public override MenuList GetMenuItems ()
		{
			// intentionally dont inherit
			MenuList list = new MenuList ();
			list[MenuListContainer.Actions].Add (
				new MenuItem ("Open Trash", Icon, (o, a) => OpenTrash ()));
			list[MenuListContainer.Actions].Add (
				new MenuItem ("Empty Trash", "gtk-clear", (o, a) => EmptyTrash (), !TrashFull));
			return list;
		}
		
		void OpenTrash ()
		{
			DockServices.System.Open (OwnedFile);
		}
		
		void EmptyTrash ()
		{
			string message = Catalog.GetString ("<big><b>Empty all of the items from the trash?</b></big>\n\n" + 
			                                    "If you choose to empty the trash, all items in it\nwill be permanently lost. " + 
			                                    "Please note that you\ncan also delete them separately.");
			Gtk.MessageDialog md = new Gtk.MessageDialog (null, 
												  Gtk.DialogFlags.Modal,
												  Gtk.MessageType.Warning, 
												  Gtk.ButtonsType.None,
												  message);
			md.Modal = false;
			md.AddButton ("_Cancel", Gtk.ResponseType.Cancel);
			md.AddButton ("Empty _Trash", Gtk.ResponseType.Ok);

			md.Response += (o, args) => {
				if (args.ResponseId != Gtk.ResponseType.Cancel ) {
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
				md.Destroy ();
			};
			
			md.Show ();
		}
	}
}
