//  
// Copyright (C) 2009 Robert Dyer
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;

using Gtk;
using Mono.Unix;

namespace WeatherDocklet
{
	public class WeatherConfigurationDialog : Dialog
	{
		public static WeatherConfigurationDialog instance;
		
		public WeatherConfigurationDialog ()
		{
			SkipTaskbarHint = true;
			TypeHint = Gdk.WindowTypeHint.Dialog;
			WindowPosition = Gtk.WindowPosition.Center;
			KeepAbove = true;
			Stick ();
			
			Title = Catalog.GetString ("Weather Configuration");
			IconName = Gtk.Stock.Preferences;
			
			WeatherConfiguration config = new WeatherConfiguration ();
			
			VBox.PackEnd (config);
			VBox.ShowAll ();
			
            Gtk.Button close_button = new Gtk.Button();
            close_button.CanFocus = true;
            close_button.Name = "close_button";
            close_button.UseStock = true;
            close_button.UseUnderline = true;
            close_button.Label = "gtk-close";
			close_button.Show ();
			AddActionWidget (close_button, ResponseType.Close);
			SetDefaultSize (350, 400);
		}
		
		protected override void OnResponse (ResponseType response_id)
		{
			Hide ();
		}
	}
}
