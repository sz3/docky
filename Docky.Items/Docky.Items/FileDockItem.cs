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

using Cairo;
using Gdk;

using Docky.Menus;

using Docky.Services;

namespace Docky.Items
{


	public class FileDockItem : IconDockItem
	{
		public static FileDockItem NewFromUri (string uri)
		{
			string path = new Uri (uri).LocalPath;
			if (!Directory.Exists (path) && !File.Exists (path)) {
				return null;
			}
			
			return new FileDockItem (uri);
		}
		
		string uri;
		
		FileDockItem (string uri)
		{
			Gnome.IconLookupResultFlags results;
			Icon = Gnome.Icon.LookupSync (Gtk.IconTheme.Default, null, uri, null, 0, out results);
			
			HoverText = System.IO.Path.GetFileName (new Uri (uri).LocalPath);
			this.uri = uri;
		}
		
		public override string UniqueID ()
		{
			return uri;
		}

		
		protected override ClickAnimation OnClicked (uint button, ModifierType mod, double xPercent, double yPercent)
		{
			Open ();
			
			return ClickAnimation.Bounce;
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
			string path = System.IO.Path.GetDirectoryName (new Uri (uri).AbsolutePath);
			DockServices.System.Open (new Uri (path).AbsoluteUri);
		}
	}
}
