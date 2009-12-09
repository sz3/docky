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

using org.freedesktop.DBus;
using NDesk.DBus;

namespace Docky.DBus
{
	public delegate void MenuItemActivatedHandler (uint menuHandle);
	
	public delegate void DockItemActivatedHandler (uint menuHandle, uint button);

	[Interface ("org.gnome.Docky.Item")]
	public interface IDockyDBusItem
	{
		string Name { get; }
		string Text { get; }
		string Icon { get; }
		
		bool OwnsDesktopFile { get; }
		string DesktopFile { get; }
		
		event MenuItemActivatedHandler MenuItemActivated;

		uint AddMenuItem (string name, string icon, string title);
		
		void RemoveItem (uint item);
		
		void ConfirmItem (uint item);
	}
}
