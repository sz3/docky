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
using System.Text.RegularExpressions;

using Mono.Unix;
using Mono.Addins;

using Docky.Widgets;
using Docky.Items;
using Docky.Services;

namespace Docky
{

	public class DockletTile : AbstractTileObject
	{
		public Addin Addin { get; private set; }
		public AbstractDockItemProvider Provider { get; private set; }
		Gtk.Button ConfigButton;

		public DockletTile (string addinID) : this (addinID, null)
		{
		}
		
		public DockletTile (string addinID, AbstractDockItemProvider provider)
		{
			Addin = PluginManager.AddinFromID (addinID);
			Provider = provider;
			
			ButtonStateDisabledText = Catalog.GetString ("_Add");
			ButtonStateEnabledText = Catalog.GetString ("_Remove");
			SubDescriptionTitle = Catalog.GetString ("Author");
			Enabled = Addin.Enabled;
			
			Name = Addin.Name;
			Description = Addin.Description.Description;
			SubDescriptionText = Addin.Description.Author;
			
			ConfigButton = new Gtk.Button (Catalog.GetString ("Settings"));
			ConfigButton.Clicked += delegate {
				PluginManager.ConfigForAddin  (Addin.Id).Show ();
			};
			
			UpdateInfo ();
		}
		
		void UpdateInfo ()
		{
			if (Enabled && PluginManager.ConfigForAddin (Addin.Id) != null)
				AddUserButton (ConfigButton);
			else
				RemoveUserButton (ConfigButton);
			
			if (Provider == null)
				Icon = PluginManager.DefaultPluginIcon;
			else
				Icon = Provider.Icon;
		}
		
		public override void OnActiveChanged ()
		{
			Enabled = !Enabled;
			
			if (Enabled) {
				Provider = PluginManager.Enable (Addin);
				ConfigurationWindow.ActiveDock.Preferences.AddProvider (Provider);
			} else {
				PluginManager.Disable (Addin);
				ConfigurationWindow.ActiveDock.Preferences.RemoveProvider (Provider);
				Provider = null;
			}
			
			UpdateInfo ();
		}
	}
}