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

using Docky.Menus;

using Docky.Services;

namespace Docky.Items
{


	public class FileDockItem : IconDockItem
	{
		public static FileDockItem NewFromUri (string uri)
		{
			string path = Gnome.Vfs.Global.GetLocalPathFromUri (uri);
			if (!Directory.Exists (path) && !File.Exists (path)) {
				return null;
			}
			
			return new FileDockItem (uri);
		}
		
		static string IconNameForPath (string path)
		{
			string home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			
			if (path == home)
				return "folder-home";
			
			if (path.StartsWith (home)) {
				// add one due to the path separtor
				switch (path.Substring (home.Length + 1)) {
				case "Desktop":
					return "desktop";
				case "Pictures":
					return "folder-pictures";
				case "Music":
					return "folder-music";
				case "Documents":
					return "folder-documents";
				case "Downloads":
					return "folder-download";
				case "Public":
					return "folder-publicshare";
				case "Videos":
					return "folder-video";
				}
			}
			
			return "folder";
		}
		
		string uri;
		bool is_folder;
		
		public string Uri {
			get { return uri; }
		}
		
		protected FileDockItem (string uri)
		{
			this.uri = uri;
			string path = Gnome.Vfs.Global.GetLocalPathFromUri (uri);
			
			if (!Directory.Exists (path)) {
				is_folder = false;
				Gnome.IconLookupResultFlags results;
			
				Icon = Gnome.Icon.LookupSync (Gtk.IconTheme.Default, null, uri, null, 0, out results);
			} else {
				is_folder = true;
				Icon = IconNameForPath (path);
			}
			
			HoverText = System.IO.Path.GetFileName (path);
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
			string folder = Gnome.Vfs.Global.GetLocalPathFromUri (Uri);
			foreach (string uri in uris) {
				string file = Gnome.Vfs.Global.GetLocalPathFromUri (uri);
				string newName = Path.Combine (folder, Path.GetFileName (file));
				if (File.Exists (file) && !File.Exists (newName) && !Directory.Exists (newName)) {
					File.Move (file, Path.Combine (folder, Path.GetFileName (file)));
				} else if (Directory.Exists (file) && !File.Exists (newName) && !Directory.Exists (newName)) {
					Directory.Move (file, newName);
				}
			}
			return true;
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
		
		void Open ()
		{
			DockServices.System.Open (uri);
		}
		
		void OpenContainingFolder ()
		{
			// retarded but works. Must be a better way
			string path = System.IO.Path.GetDirectoryName (Gnome.Vfs.Global.GetLocalPathFromUri (uri));
			DockServices.System.Open (Gnome.Vfs.Global.GetUriFromLocalPath (path));
		}
	}
}
