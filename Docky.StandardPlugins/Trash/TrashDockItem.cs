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
using System.IO;
using System.Linq;
using System.Text;

using Mono.Unix;

using GLib;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Trash
{


	public class TrashDockItem : IconDockItem
	{
		
		FileSystemWatcher fsw;

		string Trash {
			get { 
				return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "Trash/files/");
			}
		}
		
		bool TrashFull {
			get {
				return Directory.Exists (Trash) && (Directory.GetFiles (Trash).Any () || Directory.GetDirectories (Trash).Any ());
			}
		}
		
		public TrashDockItem ()
		{
			if (!Directory.Exists (Trash))
				Directory.CreateDirectory (Trash);
			
			HoverText = "Trash";
			
			UpdateIcon ();
			SetupFileSystemWatch ();
		}
		
		void SetupFileSystemWatch ()
		{
			fsw = new FileSystemWatcher (Trash);
			fsw.IncludeSubdirectories = false;
			fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;

			fsw.Changed += HandleChanged;
			fsw.Created += HandleChanged;
			fsw.Deleted += HandleChanged;
			fsw.EnableRaisingEvents = true;
		}

		void HandleChanged(object sender, FileSystemEventArgs e)
		{
			Gtk.Application.Invoke (delegate {
				UpdateIcon ();
			});
		}

		void UpdateIcon ()
		{
			if (TrashFull)
				Icon = "gnome-stock-trash-full";
			else
				Icon = "gnome-stock-trash";
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
			return FileFactory.NewForUri (uri).Exests;
		}
		
		bool ReceiveItem (string uri)
		{
			bool trashed = FileFactory.NewForUri (uri).Trash (null);
			
			if (trashed) {
				UpdateIcon ();
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
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			yield return new MenuItem ("Open Trash", "user-trash", (o, a) => OpenTrash ());
			yield return new MenuItem ("Empty Trash", "gtk-delete", (o, a) => EmptyTrash (), !TrashFull);
		}
		
		void OpenTrash ()
		{
			DockServices.System.Open ("trash://");
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
				if (args.ResponseId != Gtk.ResponseType.Cancel && Directory.Exists (Trash)) {
					fsw.Dispose ();
					fsw = null;
					
					try {
						Directory.Delete (Trash, true);
						Directory.CreateDirectory (Trash);
					} catch { /* do nothing */ }
					
					SetupFileSystemWatch ();
					
					Gtk.Application.Invoke (delegate {
						UpdateIcon ();
						OnPaintNeeded ();
					});
				}
				md.Destroy ();
			};
			
			md.Show ();
		}
	}
}
