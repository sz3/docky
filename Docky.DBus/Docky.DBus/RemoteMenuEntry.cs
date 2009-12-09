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

namespace Docky.DBus
{


	public class RemoteMenuEntry : RemoteItem
	{
		public event EventHandler Activated;

		public string Name { get; private set; }
		public string Icon { get; private set; }
		public string Title { get; private set; }

		public RemoteMenuEntry (uint id, string name, string icon, string title) : base(id)
		{
			Name = name;
			Icon = icon;
			Title = title;
		}

		public void OnActivated ()
		{
			if (Activated != null)
				Activated (this, EventArgs.Empty);
		}
	}
}
