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
		
		Dock activeDock;
		string AutoStartKey = "Hidden";
		DesktopItem autostartfile;
		
		Dock ActiveDock {
			get { return activeDock; }
			set {
				if (activeDock == value)
					return;
				
				if (activeDock != null)
					activeDock.UnsetActiveGlow ();
				
				activeDock = value;
				
				if (activeDock != null)
					activeDock.SetActiveGlow ();
			}
		}
		
		public ConfigurationWindow () : base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			
			SkipTaskbarHint = true;
			
			int i = 0;
			foreach (string theme in Docky.Controller.DockThemes) {
				theme_combo.AppendText (theme);
				if (Docky.Controller.DockTheme == theme) {
					theme_combo.Active = i;
				}
				i++;
			}
			
			if (Docky.Controller.Docks.Count () == 1)
				ActiveDock = Docky.Controller.Docks.First ();
			
			SetupConfigAlignment ();
			
			start_with_computer_checkbutton.Active = IsAutoStartEnabled ();
			
			CheckButtons ();
			
			ShowAll ();
		}
		
		protected override bool OnDeleteEvent (Event evnt)
		{
			Hide ();
			ActiveDock = null;
			SetupConfigAlignment ();
			return true;
		}

		protected virtual void OnCloseButtonClicked (object sender, System.EventArgs e)
		{
			Hide ();
			ActiveDock = null;
			SetupConfigAlignment ();
		}
		
		void SetupConfigAlignment ()
		{
			if (config_alignment.Child != null) {
				config_alignment.Remove (config_alignment.Child);
			}
			
			if (ActiveDock == null) {
				VBox vbox = new VBox ();
				
				HBox hboxTop = new HBox ();
				HBox hboxBottom = new HBox ();
				Label label1 = new Gtk.Label (Mono.Unix.Catalog.GetString ("Click on any dock to configure."));
				Label label2 = new Gtk.Label (Mono.Unix.Catalog.GetString ("Drag any dock to move it."));
				
				vbox.Add (hboxTop);
				vbox.Add (label1);
				vbox.Add (label2);
				vbox.Add (hboxBottom);
				
				vbox.SetChildPacking (hboxTop, true, true, 0, PackType.Start);
				vbox.SetChildPacking (label1, false, false, 0, PackType.Start);
				vbox.SetChildPacking (label2, false, false, 0, PackType.Start);
				vbox.SetChildPacking (hboxBottom, true, true, 0, PackType.Start);
				
				config_alignment.Add (vbox);
			} else {
				config_alignment.Add (ActiveDock.PreferencesWidget);
			}
			config_alignment.ShowAll ();
		}

		protected override void OnShown ()
		{
			foreach (Dock dock in Docky.Controller.Docks) {
				dock.EnterConfigurationMode ();
				dock.ConfigurationClick += HandleDockConfigurationClick;
			}
			
			if (Docky.Controller.Docks.Count () == 1) {
				ActiveDock = Docky.Controller.Docks.First ();
				SetupConfigAlignment ();
			}
			
			KeepAbove = true;
			Stick ();
			
			base.OnShown ();
		}

		void HandleDockConfigurationClick (object sender, EventArgs e)
		{
			Dock dock = sender as Dock;
			
			if (ActiveDock != dock) {
				ActiveDock = dock;
				SetupConfigAlignment ();
				CheckButtons ();
			}
		}

		protected override void OnHidden ()
		{
			foreach (Dock dock in Docky.Controller.Docks) {
				dock.ConfigurationClick -= HandleDockConfigurationClick;
				dock.LeaveConfigurationMode ();
				dock.UnsetActiveGlow ();
			}
			base.OnHidden ();
		}

		protected virtual void OnThemeComboChanged (object sender, System.EventArgs e)
		{
			Docky.Controller.DockTheme = theme_combo.ActiveText;
			if (Docky.Controller.NumDocks == 1) {
				ActiveDock = null;
				SetupConfigAlignment ();
				CheckButtons ();
			}
		}
	
		protected virtual void OnDeleteDockButtonClicked (object sender, System.EventArgs e)
		{
			if (!(Docky.Controller.Docks.Count () > 1))
				return;
			
			if (ActiveDock != null) {
				Docky.Controller.DeleteDock (ActiveDock);
				if (Docky.Controller.Docks.Count () == 1)
					ActiveDock = Docky.Controller.Docks.First ();
				else
					ActiveDock = null;
				SetupConfigAlignment ();
			}
			CheckButtons ();
		}
		
		protected virtual void OnNewDockButtonClicked (object sender, System.EventArgs e)
		{
			Dock newDock = Docky.Controller.CreateDock ();
			
			if (newDock != null) {
				newDock.ConfigurationClick += HandleDockConfigurationClick;
				newDock.EnterConfigurationMode ();
				ActiveDock = newDock;
				SetupConfigAlignment ();
			}
			CheckButtons ();
		}
		
		void CheckButtons ()
		{
			int spotsAvailable = 0;
			for (int i = 0; i < Screen.Default.NMonitors; i++)
				spotsAvailable += Docky.Controller.PositionsAvailableForDock (i).Count ();
			
			delete_dock_button.Sensitive = (Docky.Controller.Docks.Count () == 1 || ActiveDock == null) ? false : true;
			new_dock_button.Sensitive = (spotsAvailable == 0) ? false : true;
		}

		string AutoStartDir {
			get { return System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "autostart"); }
		}

		string AutoStartFileName {
			get { return System.IO.Path.Combine (AutoStartDir, "docky.desktop"); }
		}

		string AutoStartUri {
			get { return Gnome.Vfs.Uri.GetUriFromLocalPath (AutoStartFileName); }
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
					autostartfile = DesktopItem.NewFromFile (System.IO.Path.Combine (AssemblyInfo.InstallData, "applications/docky.desktop"), DesktopItemLoadFlags.NoTranslations);
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
				return !String.Equals (autostart.GetString (AutoStartKey), "true", StringComparison.OrdinalIgnoreCase);
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
		
		protected virtual void OnStartWithComputerCheckbuttonToggled (object sender, System.EventArgs e)
		{
			SetAutoStartEnabled (start_with_computer_checkbutton.Active);
		}
	
	}
}
