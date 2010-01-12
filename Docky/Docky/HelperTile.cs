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
using System.Text.RegularExpressions;

using Mono.Unix;

using Docky.Widgets;
using Docky.Services;

namespace Docky
{


	public class HelperTile : AbstractTileObject
	{
		public Helper Helper { get; private set; }

		public HelperTile (Helper helper)
		{
			this.Helper = helper;
			Helper.HelperStatusChanged += delegate(object sender, HelperStatusChangedEventArgs e) {
				SetProps ();
			};
			
			ButtonStateDisabledText = Catalog.GetString ("_Enable");
			ButtonStateEnabledText = Catalog.GetString ("_Disable");
			SubDescriptionTitle = Catalog.GetString ("Status");
			
			Name = ((string) Helper.File.Basename).Split ('.')[0];
			Name = Regex.Replace (Name, "_(?<char>.)", " $1");
			Description = Helper.File.Path;
			Icon = "extension";
			
			if (helper.Data != null) {
				if (!string.IsNullOrEmpty (helper.Data.Name))
					Name = helper.Data.Name;
				if (!string.IsNullOrEmpty (helper.Data.Description))
					Description = helper.Data.Description;
				if (helper.Data.Icon != null)
					ForcePixbuf = helper.Data.Icon;
			}
			
			if (helper.IsUser) {
				Gtk.Button uninstall_btn = new Gtk.Button ();
				uninstall_btn.Label = Catalog.GetString ("Uninstall");
				uninstall_btn.Clicked += delegate {
					DockServices.Helpers.UninstallHelper (helper);
				};
				AddUserButton (uninstall_btn);
			}
			
			SetProps ();
		}
		
		void SetProps ()
		{
			SubDescriptionText = Helper.IsRunning ? Catalog.GetString ("Running") : Catalog.GetString ("Stopped");
			Enabled = Helper.Enabled;
		}
		
		public override void OnActiveChanged ()
		{
			Helper.Enabled = !Enabled;
			SetProps ();
		}
	}
}
