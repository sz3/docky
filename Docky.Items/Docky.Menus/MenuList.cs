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
		Header,
		Actions,
		Windows,
		RelatedItems,
		CustomOne,
		CustomTwo,
		Footer,
	}
	
	public class MenuList
	{
		Dictionary<MenuListContainer, List<MenuItem>> list;
		
		public IEnumerable<MenuItem> DisplayItems {
			get {
				bool separate = false;
				foreach (MenuListContainer container in list.Keys.OrderBy (key => (int) key)) {
					if (!list[container].Any ())
						continue;
					
					if (separate)
						yield return new SeparatorMenuItem ();
					foreach (MenuItem item in list[container])
						yield return item;
					separate = true;
				}
			}
		}
		
		public MenuList ()
		{
			list = new Dictionary<MenuListContainer, List<MenuItem>> ();
		}
		
		public List<MenuItem> this[MenuListContainer container]
		{
			get {
				if (!list.ContainsKey (container))
					list[container] = new List<MenuItem> ();
				return list[container];
			}
		}
		
		public bool Any ()
		{
			return list.Values.Any (sl => sl.Any ());
		}
		
		public int Count ()
		{
			return list.Values.Count ();
		}
	}
}
