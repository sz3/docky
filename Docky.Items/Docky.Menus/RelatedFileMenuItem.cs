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
using System.IO;
using System.Text;

namespace Docky.Menus
{


	public class RelatedFileMenuItem : MenuItem
	{
		public string Uri { get; private set; }
		
		public RelatedFileMenuItem (string uri)
		{
			Uri = uri;
			
			Gnome.IconLookupResultFlags results;
			Icon = Gnome.Icon.LookupSync (Gtk.IconTheme.Default, null, uri, null, 0, out results);
			
			if (uri.StartsWith ("file://"))
				Text = Path.GetFileName (System.Uri.UnescapeDataString (new Uri (uri).AbsolutePath));
			else if (uri.StartsWith ("http://"))
				Text = new Uri (uri).Host;
			else
				Text = System.Uri.UnescapeDataString (uri);
		}
	}
}
