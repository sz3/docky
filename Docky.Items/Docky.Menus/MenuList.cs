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

namespace Docky.Menus
{
	public enum MenuListContainer {
		Actions,
		Windows,
		RelatedItems,
	}
	
	public class MenuList
	{
		Dictionary<MenuListContainer, List<MenuItem>> list;
		
		public MenuList ()
		{
			list = new Dictionary<MenuListContainer, List<MenuItem>> ();
			foreach (MenuListContainer container in Enum.GetValues (typeof(MenuListContainer))) {
				list[container] = new List<MenuItem> ();
			}
		}
		
		public List<MenuItem> this[MenuListContainer container]
		{
			get {
				return list[container];
			}
		}
		
		public bool Any ()
		{
			return list.Values.Any (sl => sl.Any ());
		}
	}
}
