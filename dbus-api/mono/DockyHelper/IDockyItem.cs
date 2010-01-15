//  
//  Copyright (C) 2010 Chris Szikszoy
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

using NDesk.DBus;
using org.freedesktop.DBus;

namespace DockyHelper
{
	public struct ItemTuple
	{
		public string Name;
		public string Icon;
		public string Title;
		
		public ItemTuple (string name, string icon, string title)
		{
			Name = name;
			Icon = icon;
			Title = title;
		}
	}	
	
	public delegate void MenuItemActivatedHandler (uint menuHandle);
	
	[Interface("org.gnome.Docky.Item")]
	public interface IDockyItem : org.freedesktop.DBus.Properties
	{
		uint AddFileMenuItem (string uri, string title);
		uint AddMenuItem (string name, string icon, string title);
		void ConfirmItem (uint item);
		ItemTuple GetItem (uint item);
		void RemoveItem (uint item);
		
		void ResetBadgeText ();
		void ResetIcon ();
		void ResetText ();
		void SetAttention ();
		void SetWaiting ();
		void UnsetAttention ();
		void UnsetWaiting ();
		
		event Action ItemConfirmationNeeded;
		event MenuItemActivatedHandler MenuItemActivated;
		
		// properties
		bool OwnsDesktopFile { get; }
		bool OwnsUri { get; }
		bool CanSetIcon { get; }
		bool Attention { get; }
		bool Wait { get; }
		
		string BadgeText { get; set; }
		string Text { get; set; }
		string Icon { get; set; }
		
		string Name { get; }
		string DesktopFile { get; }
		string Uri { get; }
	}
}

