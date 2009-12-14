//  
//  Copyright (C) 2009 Chris Szikszoy
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

using GLib;

using Docky.Menus;
using Docky.Services;

namespace Docky.DBus
{

	public class RemoteFileMenuEntry : RemoteMenuEntry
	{

		public RemoteFileMenuEntry (uint id, GLib.File file, string groupTitle) : base (id, file.Basename, "", groupTitle)
		{
			Clicked += delegate {
				DockServices.System.Open (file);
			};
			
			// only check the icon if it's mounted (ie: .Path != null)
			if (!string.IsNullOrEmpty (file.Path)) {
				string thumbnailPath = file.QueryStringAttr ("thumbnail::path");
				if (string.IsNullOrEmpty (thumbnailPath))
					Icon = DockServices.Drawing.IconFromGIcon (file.Icon ());
				else
					Icon = thumbnailPath;
			} else {
				Icon = "gtk-file";
			}
		}
	}
}
