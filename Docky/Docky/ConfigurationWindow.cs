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
using System.IO;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Gnome;
using Gtk;

using Docky.Interface;
using Docky.Services;

namespace Docky
{


	public partial class ConfigurationWindow : Gtk.Window
	{

		DockPlacementWidget placement;
		
		public ConfigurationWindow () : base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			
			int i = 0;
			foreach (string theme in Docky.Controller.DockThemes) {
				theme_combo.AppendText (theme);
				if (Docky.Controller.DockTheme == theme) {
					theme_combo.Active = i;
				}
				i++;
			}
			
			checkbutton1.Active = IsAutoStartEnabled ();
			checkbutton1.Toggled += OnCheckbutton1Toggled;
			
			placement = new DockPlacementWidget (Docky.Controller.Docks);
			placement.ActiveDockChanged += PlacementActiveDockChanged;
			
			dock_pacement_align.Add (placement);
			
			configuration_widget_notebook.RemovePage (0);
			
			foreach (Dock dock in Docky.Controller.Docks) {
				configuration_widget_notebook.Add (dock.PreferencesWidget);
			}
			
			ShowAll ();
		}

		void PlacementActiveDockChanged (object sender, EventArgs e)
		{
			configuration_widget_notebook.Page = 
				configuration_widget_notebook.PageNum (placement.ActiveDock.PreferencesWidget);
		}
		
		protected override bool OnDeleteEvent (Event evnt)
		{
			Hide ();
			
			return true;
		}

		protected virtual void OnCloseButtonClicked (object sender, System.EventArgs e)
		{
			Hide ();
		}


		protected virtual void OnDeleteButtonClicked (object sender, System.EventArgs e)
		{
			configuration_widget_notebook.Remove (placement.ActiveDock.PreferencesWidget);
			Docky.Controller.DeleteDock (placement.ActiveDock);
			placement.SetDocks (Docky.Controller.Docks);
		}

		protected virtual void OnAddButtonClicked (object sender, System.EventArgs e)
		{
			Dock dock = Docky.Controller.CreateDock ();
			if (dock == null)
				return;
			
			configuration_widget_notebook.Add (dock.PreferencesWidget);
			placement.SetDocks (Docky.Controller.Docks);
			
			placement.ActiveDock = dock;
		}

		protected virtual void OnThemeComboChanged (object sender, System.EventArgs e)
		{
			Docky.Controller.DockTheme = theme_combo.ActiveText;
		}
		
		protected virtual void OnCheckbutton1Toggled (object sender, System.EventArgs e)
		{
			SetAutoStartEnabled (checkbutton1.Active);
		}
		
		string AutoStartKey = "Hidden";
		DesktopItem autostartfile;
		
		string AutoStartDir {
			get {
				return System.IO.Path.Combine (
					Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "autostart");
		    }
		}
		
		string AutoStartFileName {
		  get {
		      return System.IO.Path.Combine (AutoStartDir, "docky.desktop");
		    }
		}
		
		string AutoStartUri {
			get {
				return Gnome.Vfs.Uri.GetUriFromLocalPath (AutoStartFileName);
			}
		}
		
		DesktopItem AutoStartFile {
			get {
				if (autostartfile != null) 
					return autostartfile;
				
				try {
					autostartfile = DesktopItem.NewFromUri (AutoStartUri, DesktopItemLoadFlags.NoTranslations);
				} catch (GLib.GException loadException) {
					Log<DockPlacementWidget>.Info ("Unable to load existing autostart file: {0}", loadException.Message);
					Log<DockPlacementWidget>.Info ("Writing new autostart file to {0}", AutoStartFileName);
					autostartfile = DesktopItem.NewFromFile (System.IO.Path.Combine (AssemblyInfo.InstallData, "applications/docky.desktop"),
					                                         DesktopItemLoadFlags.NoTranslations);
					try {
						if (!Directory.Exists (AutoStartDir))
							Directory.CreateDirectory (AutoStartDir);

						autostartfile.Save (AutoStartUri, true);
						autostartfile.Location = AutoStartUri;
					} catch (Exception e) {
						Log<DockPlacementWidget>.Error ("Failed to write initial autostart file: {0}", e.Message);
					}
				}
				return autostartfile;
			}
		}
		
		bool IsAutoStartEnabled ()
		{
			DesktopItem autostart = AutoStartFile;
			
			if (!autostart.Exists ()) {
				Log<SystemService>.Error ("Could not open autostart file {0}", AutoStartUri);
			}
			
			if (autostart.AttrExists (AutoStartKey)) {
				return !String.Equals(autostart.GetString (AutoStartKey), "true", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
		
		void SetAutoStartEnabled (bool enabled)
		{
			DesktopItem autostart = AutoStartFile;
			
			autostart.SetBoolean (AutoStartKey, !enabled);
			try {
				autostart.Save (null, true);
			} catch (Exception e) {
				Log<SystemService>.Error ("Failed to update autostart file: {0}", e.Message);
			}
		}
	}
}
