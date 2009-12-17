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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using GLib;
using Mono.Unix;
using Notifications;

using Docky.Menus;
using Docky.Services;

namespace Docky.Items
{
	public class FileDockItem : ColoredIconDockItem
	{
		public static FileDockItem NewFromUri (string uri)
		{
			// FIXME: need to do something with this... .Exists will fail for non native files
			// but they are still valid file items (like an unmounted ftp://... file)
			// even File.QueryExists () will return false for valid files (ftp://) that aren't mounted.
			/*
			string path = Gnome.Vfs.Global.GetLocalPathFromUri (uri);
			if (!Directory.Exists (path) && !File.Exists (path)) {
				return null;
			}
			*/
			return new FileDockItem (uri);
		}
		
		public static FileDockItem NewFromUri (string uri, string force_hover_text, string backup_icon)
		{
			return new FileDockItem (uri, force_hover_text, backup_icon);
		}
		
		const string ThumbnailPathKey = "thumbnail::path";
		const string FilesystemIDKey = "id::filesystem";
		const string FilesystemFreeKey = "filesystem::free";
		string uri;
		bool is_folder;
		string forced_hover_text = null;
		string backup_icon = null;
		
		public string Uri {
			get { return uri; }
		}
		
		public File OwnedFile { get; private set; }
		
		protected FileDockItem (string uri)
		{			
			this.uri = uri;
			OwnedFile = FileFactory.NewForUri (uri);
			
			// update this file on successful mount
			OwnedFile.AddMountAction (() => {
				SetIconFromGIcon (OwnedFile.Icon ());
				OnPaintNeeded ();
			});
			
			UpdateInfo ();
		}
		
		protected FileDockItem (string uri, string forced_hover, string backupIcon) : this (uri)
		{
			forced_hover_text = forced_hover;
			backup_icon = backupIcon;
			UpdateInfo ();
		}
		
		// this should be called after a successful mount of the file
		public void UpdateInfo ()
		{
			if (OwnedFile.QueryFileType (0, null) == FileType.Directory)
				is_folder = true;
			else
				is_folder = false;
			
			// only check the icon if it's mounted (ie: .Path != null)
			if (!string.IsNullOrEmpty (OwnedFile.Path)) {
				string thumbnailPath = OwnedFile.QueryStringAttr (ThumbnailPathKey);
				if (string.IsNullOrEmpty (thumbnailPath))
					SetIconFromGIcon (OwnedFile.Icon ());
				else
					Icon = thumbnailPath;
			} else if (!string.IsNullOrEmpty (backup_icon)) {
				Icon = backup_icon;
			} else {
				Icon = "";
			}
			
			if (string.IsNullOrEmpty (forced_hover_text))
			    HoverText = OwnedFile.Basename;
			else
				HoverText = forced_hover_text;
			
			OnPaintNeeded ();
		}
		
		public override string UniqueID ()
		{
			return uri;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (Icon == null)
				return;
			base.OnScrolled (direction, mod);
		}

		protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			bool can_write;
			// this could fail if we try to call it on an unmounted location
			try {
				can_write = OwnedFile.QueryBoolAttr ("access::can-write");
			} catch {
				can_write = false;
			}
			
			// only accept the drop if it's a folder, and we can write to it.
			return is_folder && can_write;
		}

		protected override bool OnAcceptDrop (IEnumerable<string> uris)
		{
			Notification note = null;
			foreach (File file in uris.Select (uri => FileFactory.NewForUri (uri))) {
				try {
					if (!file.Exists)
						continue;
					
					// gather some information first
					long fileSize = file.GetSize ();
					ulong freeSpace = OwnedFile.QueryULongAttr (FilesystemFreeKey);
					if ((ulong) fileSize > freeSpace)
						throw new Exception (Catalog.GetString ("Not enough free space on destination."));
					
					string ownedFSID = OwnedFile.QueryStringAttr (FilesystemIDKey);
					string destFSID = file.QueryStringAttr (FilesystemIDKey);
					
					string nameAfterMove = NewFileName (OwnedFile, file);
					
					DockServices.System.RunOnThread (()=> {
						
						bool performing = true;
						long cur = 0, tot = 10;
						
						note = Docky.Services.Log.Notify ("", DockServices.Drawing.IconFromGIcon (file.Icon ()), "{0}% " + Catalog.GetString ("Complete") + "...", cur / tot);
						GLib.Timeout.Add (250, () => {
							note.Body = string.Format ("{0}% ", string.Format ("{0:00.0}", ((float) Math.Min (cur, tot) / tot) * 100)) + Catalog.GetString ("Complete") + "...";
							return performing;
						});
						
						// check the filesystem IDs, if they are the same, we move, otherwise we copy.
						if (ownedFSID == destFSID) {
							note.Summary = Catalog.GetString ("Moving") + string.Format (" {0}...", file.Basename);
							file.Move (OwnedFile.GetChild (nameAfterMove), FileCopyFlags.NofollowSymlinks | FileCopyFlags.AllMetadata | FileCopyFlags.NoFallbackForMove, null, (current, total) => {
								cur = current;
								tot = total;
							});
						} else {
							note.Summary = Catalog.GetString ("Copying") + string.Format (" {0}...", file.Basename);
							file.Copy_Recurse (OwnedFile.GetChild (nameAfterMove), 0, (current, total) => {
								cur = current;
								tot = total;
							});
						}
						
						performing = false;
						note.Body = string.Format ("100% {0}.", Catalog.GetString ("Complete"));
					});
					// until we use a new version of GTK# which supports getting the GLib.Error code
					// this is about the best we can do.
				} catch (Exception e) {
					Docky.Services.Log.Notify (Catalog.GetString ("Error performing drop action"), Gtk.Stock.DialogError, e.Message);
					Log<FileDockItem>.Error ("{0}: {1}", Catalog.GetString ("Error performing drop action"), e.Message);
					Log<FileDockItem>.Debug (e.StackTrace);
					
					if (note != null)
						note.Close ();
				}
			}			
			return true;
		}
		
		string NewFileName (File dest, File fileToMove)
		{
			string name, ext;
						
			if (fileToMove.Basename.Split ('.').Count() > 1) {
				name = fileToMove.Basename.Split ('.').First ();
				ext = fileToMove.Basename.Substring (fileToMove.Basename.IndexOf ('.'));
			} else {
				name = fileToMove.Basename;
				ext = "";
			}
			if (dest.GetChild (fileToMove.Basename).Exists) {
				int i = 1;
				while (dest.GetChild (string.Format ("{0} ({1}){2}", name, i, ext)).Exists) {
					i++;
				}
				return string.Format ("{0} ({1}){2}", name, i, ext);
			} else {
				return fileToMove.Basename;
			}
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				Open ();
				return ClickAnimation.Bounce;
			}
			return base.OnClicked (button, mod, xPercent, yPercent);
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			list[MenuListContainer.Actions].Insert (0, new MenuItem (Catalog.GetString ("Open"), "gtk-open", (o, a) => Open ()));
			list[MenuListContainer.Actions].Insert (1, new MenuItem (Catalog.GetString ("Open Containing Folder"), "folder", (o, a) => OpenContainingFolder (), OwnedFile.Parent == null));
			return list;
		}
		
		protected void Open ()
		{
			DockServices.System.Open (OwnedFile);
		}
		
		protected void OpenContainingFolder ()
		{
			DockServices.System.Open (OwnedFile.Parent);
		}
	}
}
