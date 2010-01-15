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

namespace DockyHelper
{

	public class MenuItem : BaseMenuItem
	{
		public string Name { get; private set; }
		public string Icon { get; private set; }
		
		public MenuItem (string name, string icon, string title)
		{
			this.Name = name;
			this.Icon = icon;
			this.Title = title;
		}
	}
}

