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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using GLib;
using Notifications;

using Docky.Menus;
using Docky.Services;

namespace Docky.Items
{


	public class FileDockItem : IconDockItem
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
		
		const string ThumbnailPathKey = "thumbnail::path";
		const string FilesystemIDKey = "id::filesystem";
		static IPreferences prefs = DockServices.Preferences.Get <FileDockItem> ();
		static Regex hueRegex = new Regex ("[^a-zA-Z0-9]");
		string uri;
		bool is_folder;
		
		public string Uri {
			get { return uri; }
		}
		
		public File OwnedFile { get; private set; }
		
		protected FileDockItem (string uri)
		{
			IconUpdated += HandleIconUpdated;
			
			this.uri = uri;
			OwnedFile = FileFactory.NewForUri (uri);
			
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
				string thumbnailPath = OwnedFile.QueryInfo (ThumbnailPathKey, FileQueryInfoFlags.NofollowSymlinks, 
					null).GetAttributeAsString (ThumbnailPathKey);
				if (string.IsNullOrEmpty (thumbnailPath))
					SetIconFromGIcon (OwnedFile.Icon ());
				else
					Icon = thumbnailPath;
			} else {
				Icon = "";
			}
			
			HoverText = OwnedFile.Basename;
			
			OnPaintNeeded ();
		}
		
		public override string UniqueID ()
		{
			return uri;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (Icon == null || !(Icon.Contains ("inode-directory") || Icon.Contains ("folder")))
				return;
			
			if (direction == Gdk.ScrollDirection.Up)
				HueShift += 5;
			else if (direction == Gdk.ScrollDirection.Down)
				HueShift -= 5;
			
			if (HueShift < 0)
				HueShift += 360;
			HueShift %= 360;
			
			prefs.Set<double> (hueRegex.Replace (UniqueID (), "_"), HueShift);
		}
		
		protected void ResetHue ()
		{
			HueShift = 0;
			prefs.Set<double> (hueRegex.Replace (UniqueID (), "_"), HueShift);
		}

		protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			return is_folder;
		}

		protected override bool OnAcceptDrop (IEnumerable<string> uris)
		{
			foreach (string uri in uris) {
				try {
					File file = FileFactory.NewForUri (uri);
					if (!file.Exists)
						continue;
					
					string ownedFSID = OwnedFile.QueryInfo (FilesystemIDKey, FileQueryInfoFlags.NofollowSymlinks, null)
						.GetAttributeAsString (FilesystemIDKey);
					string destFSID = file.QueryInfo (FilesystemIDKey, FileQueryInfoFlags.NofollowSymlinks, null)
						.GetAttributeAsString (FilesystemIDKey);
					
					string nameAfterMove = NewFileName (OwnedFile, file);
					DockServices.System.RunOnThread (()=> {
						Notification note;
						bool performing = true;
						long cur = 0, tot = 10;
						
						note = Docky.Services.Log.Notify ("", DockServices.Drawing.IconFromGIcon (file.Icon ()), "{0}% Complete.", cur / tot);
						GLib.Timeout.Add (250, () => {
							note.Body = string.Format ("{0}% Complete.", string.Format ("{0:00.0}", ((float) Math.Min (cur, tot) / tot) * 100));
							return performing;
						});
						
						// check the filesystem IDs, if they are the same, we move, otherwise we copy.
						if (ownedFSID == destFSID) {
							note.Summary = string.Format ("Moving {0}", file.Basename);
							file.Move (OwnedFile.GetChild (nameAfterMove), FileCopyFlags.NofollowSymlinks | FileCopyFlags.AllMetadata | FileCopyFlags.NoFallbackForMove, null, (current, total) => {
								cur = current;
								tot = total;
							});
						} else {
							note.Summary = string.Format ("Copying {0}", file.Basename);
							file.Copy_Recurse (OwnedFile.GetChild (nameAfterMove), 0, (current, total) => {
								cur = current;
								tot = total;
							});
						}
						
						performing = false;
						note.Body = "100% Complete.";
					});
				} catch (GException ex) {
					
					continue;
				} catch (Exception e) {
				}
			}			
			return true;
		}
		
		string NewFileName (File dest, File fileToMove)
		{
			string name, ext;
			
			if (fileToMove.Basename.Split ('.').Count() > 0) {
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
			list[MenuListContainer.Actions].Add (new MenuItem ("Open", "gtk-open", (o, a) => Open ()));
			list[MenuListContainer.Actions].Add (new MenuItem ("Open Containing Folder", "folder", (o, a) => OpenContainingFolder ()));
			if (HueShift != 0)
				list[MenuListContainer.Actions].Add (new MenuItem ("Reset Color", "edit-clear", (o, a) => ResetHue ()));
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
		
		protected virtual void HandleIconUpdated (object obj, EventArgs args)
		{
			if (Icon != null && (Icon.Contains ("inode-directory") || Icon.Contains ("folder")))
				HueShift = prefs.Get<double> (hueRegex.Replace (UniqueID (), "_"), 0);
		}
	}
}
