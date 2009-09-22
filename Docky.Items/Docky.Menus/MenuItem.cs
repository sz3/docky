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

	public class MenuItem
	{
		public event EventHandler TextChanged;
		public event EventHandler IconChanged;
		public event EventHandler Clicked;
		
		string text;
		public string Text {
			get { return text; }
			set {
				if (text == value)
					return;
				text = value;
				if (TextChanged != null)
					TextChanged (this, EventArgs.Empty);
			} 
		}
		
		string icon;
		public string Icon {
			get { return icon; }
			set {
				if (icon == value)
					return;
				icon = value;
				if (IconChanged != null)
					IconChanged (this, EventArgs.Empty);
			}
		}
		
		public void SendClick ()
		{
			if (Clicked != null)
				Clicked (this, EventArgs.Empty);
		}
		
		public MenuItem (string text, string icon)
		{
			this.icon = icon;
			this.text = text;
		}
		
		public MenuItem (string text, string icon, EventHandler onClicked) : this(text, icon)
		{
			Clicked += onClicked;
		}
	}
}
