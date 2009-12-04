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

using Gtk;

namespace Docky.Widgets
{

	public static class ExtensionMethods
	{
		public static void AddAutoTranslateButton (this Dialog d, string gtkString, ResponseType response)
		{
			Gtk.Button button = new Gtk.Button();
            button.CanFocus = true;
            button.UseStock = true;
            button.UseUnderline = true;
            button.Label = gtkString;
			button.Show ();
			d.AddActionWidget (button, response);
		}
	}
}
