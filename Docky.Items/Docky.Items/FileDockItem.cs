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

using GLib;

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
		
		string uri;
		bool is_folder;
		
		public string Uri {
			get { return uri; }
		}
		
		public File OwnedFile { get; private set; }
		
		protected FileDockItem (string uri)
		{
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
				FileInfo info = OwnedFile.QueryInfo ("*", FileQueryInfoFlags.None, null);
				SetIconFromGIcon (info.Icon);
			}
			else
				Icon = "";
			
			HoverText = OwnedFile.Basename;
			
			OnPaintNeeded ();
		}
		
		public override string UniqueID ()
		{
			return uri;
		}

		public override bool CanAcceptDrop (IEnumerable<string> uris)
		{
			return is_folder;
		}

		public override bool AcceptDrop (IEnumerable<string> uris)
		{
			foreach (string uri in uris) {
				try {
					File file = FileFactory.NewForUri (uri);
					if (!file.Exists)
						continue;
					string nameAfterMove = NewFileName (OwnedFile, file);
					file.Move (OwnedFile.GetChild (nameAfterMove), FileCopyFlags.NofollowSymlinks | FileCopyFlags.AllMetadata, null, null);
				} catch {
					continue;
				}
			}			
			return true;
		}
		
		string NewFileName (File dest, File fileToMove)
		{
			string name = fileToMove.Basename;
			
			int i = 1;
			while (dest.GetChild (name).Exists) {
				name = string.Format ("{0} ({1})", fileToMove.Basename, i);
				i++;
			}
			
			return name;
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				Open ();
				return ClickAnimation.Bounce;
			}
			return base.OnClicked (button, mod, xPercent, yPercent);
		}
		
		public override IEnumerable<MenuItem> GetMenuItems ()
		{
			yield return new MenuItem ("Open", "gtk-open", (o, a) => Open ());
			yield return new MenuItem ("Open Containing Folder", "folder", (o, a) => OpenContainingFolder ());
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
