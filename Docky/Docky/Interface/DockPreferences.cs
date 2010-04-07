//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer, Chris Szikszoy
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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.IO;
using System.Text;

using Cairo;
using Gdk;
using Gtk;

using Docky.Items;
using Docky.Services;
using System.Text.RegularExpressions;

namespace Docky.Interface
{

	[System.ComponentModel.ToolboxItem(true)]
	public partial class DockPreferences : Gtk.Bin, IDockPreferences
	{
		static T Clamp<T> (T value, T max, T min)
		where T : IComparable<T>
		{
			T result = value;
			if (value.CompareTo (max) > 0)
				result = max;
			if (value.CompareTo (min) < 0)
				result = min;
			return result;
		}

		IPreferences prefs;
		string name;
		List<AbstractDockItemProvider> item_providers;
				
		public event EventHandler PositionChanged;
		public event EventHandler PanelModeChanged;
		public event EventHandler IconSizeChanged;
		public event EventHandler AutohideChanged;
		public event EventHandler FadeOnHideChanged;
		public event EventHandler FadeOpacityChanged;
		public event EventHandler IndicatorSettingChanged;
		public event EventHandler ThreeDimensionalChanged;
		public event EventHandler ZoomEnabledChanged;
		public event EventHandler ZoomPercentChanged;
		
		public event EventHandler<ItemProvidersChangedEventArgs> ItemProvidersChanged;
		
		public FileApplicationProvider DefaultProvider { get; set; }
		
		#region Public Properties
		public IEnumerable<string> SortList {
			get { return GetOption<string[]> ("SortList", new string[0]); }
			set { SetOption<string[]> ("SortList", value.ToArray ()); }
		}
		
		public IEnumerable<AbstractDockItemProvider> ItemProviders { 
			get { return item_providers.AsEnumerable (); }
		}
		
		AutohideType hide_type;
		public AutohideType Autohide {
			get { return hide_type; }
			set {
				if (hide_type == value)
					return;
				hide_type = value;
				SetOption<string> ("Autohide", hide_type.ToString ());
				OnAutohideChanged ();
			}
		}
		
		bool? panel_mode;
		public bool PanelMode {
			get {
				if (!panel_mode.HasValue)
					panel_mode = GetOption<bool> ("PanelMode", false);
				return panel_mode.Value;
			}
			set {
				if (panel_mode == value)
					return;
				panel_mode = value;
				SetOption<bool> ("PanelMode", panel_mode.Value);
				OnPanelModeChanged ();
			}
		}
		
		bool? fade_on_hide;
		public bool FadeOnHide {
			get {
				if (!fade_on_hide.HasValue)
					fade_on_hide = GetOption<bool> ("FadeOnHide", false);
				return fade_on_hide.Value; 
			}
			set {
				if (fade_on_hide == value)
					return;
				fade_on_hide = value;
				SetOption<bool> ("FadeOnHide", fade_on_hide.Value);
				OnFadeOnHideChanged ();
			}
		}
		
		double? fade_opacity;
		public double FadeOpacity {
			get {
				if (!fade_opacity.HasValue)
					fade_opacity = GetOption<double> ("FadeOpacity", 0);
				return fade_opacity.Value; 
			}
			set {
				if (fade_opacity == value)
					return;
				fade_opacity = value;
				SetOption<double> ("FadeOpacity", fade_opacity.Value);
				OnFadeOpacityChanged ();
			}
		}
		
		DockPosition position;
		public DockPosition Position {
			get { return position; }
			set {
				if (position == value)
					return;
				position = value;
				SetOption<string> ("Position", position.ToString ());
				OnPositionChanged ();
				threedee_check.Sensitive = position == DockPosition.Bottom;
			}
		}
		
		int? icon_size;
		public int IconSize {
			get {
				if (!icon_size.HasValue) {
					icon_size = GetOption<int?> ("IconSize", 48);
				}
				return icon_size.Value;
			}
			set {
				value = Clamp (value, 128, 24);
				if (icon_size == value)
					return;
				icon_size = value;
				SetOption<int?> ("IconSize", icon_size.Value);
				OnIconSizeChanged ();
			}
		}
		
		bool? indicate_multiple_windows;
		public bool IndicateMultipleWindows {
			get {
				if (!indicate_multiple_windows.HasValue)
					indicate_multiple_windows = GetOption<bool?> ("IndicateMultipleWindows", false);
				return indicate_multiple_windows.Value;
			}
			set {
				if (indicate_multiple_windows == value)
					return;
				indicate_multiple_windows = value;
				SetOption<bool?> ("IndicateMultipleWindows", indicate_multiple_windows.Value);
				OnIndicatorSettingChanged ();
			}
		}
		
		bool? three_dimensional;
		public bool ThreeDimensional {
			get {
				if (!three_dimensional.HasValue)
					three_dimensional = GetOption<bool?> ("ThreeDimensional", false);
				return three_dimensional.Value;
			}
			set {
				if (three_dimensional == value)
					return;
				three_dimensional = value;
				SetOption<bool?> ("ThreeDimensional", three_dimensional.Value);
				OnThreeDimensionalChanged ();
			}
		}
		
		bool? zoom_enabled;
		public bool ZoomEnabled {
			get {
				if (!zoom_enabled.HasValue)
					zoom_enabled = GetOption<bool?> ("ZoomEnabled", true);
				return zoom_enabled.Value; 
			}
			set {
				if (zoom_enabled == value)
					return;
				zoom_enabled = value;
				SetOption<bool?> ("ZoomEnabled", zoom_enabled.Value);
				OnZoomEnabledChanged ();
			}
		}
				
		double? zoom_percent;
		public double ZoomPercent {
			get {
				if (!zoom_percent.HasValue)
					zoom_percent = GetOption<double?> ("ZoomPercent", 2.0);
				return zoom_percent.Value;
			}
			set {
				value = Clamp (value, 4, 1);
				if (zoom_percent == value)
					return;
				
				zoom_percent = value;
				SetOption<double?> ("ZoomPercent", zoom_percent.Value);
				OnZoomPercentChanged ();
			}
		}
		
		int? monitor_number;
		public int MonitorNumber {
			get {
				if (!monitor_number.HasValue)
					monitor_number = GetOption<int?> ("MonitorNumber", 0);
				if (monitor_number.Value >= Gdk.Screen.Default.NMonitors)
					monitor_number = 0;
				return monitor_number.Value;
			}
			set {
				if (monitor_number == value)
					return;
				monitor_number = value;
				SetOption<int?> ("MonitorNumber", monitor_number.Value);
				OnPositionChanged ();
			}
		}
		#endregion
		
		bool? window_manager;
		public bool WindowManager {
			get {
				if (!window_manager.HasValue)
					window_manager = GetOption<bool?> ("WindowManager", false);
				return window_manager.Value; 
			}
			set {
				if (value == window_manager)
					return;
				
				window_manager = value;
				SetOption<bool?> ("WindowManager", window_manager);
			}
		}
		
		IEnumerable<string> Launchers {
			get {
				return GetOption<string[]> ("Launchers", new string[0]).AsEnumerable ();
			}
			set {
				SetOption<string[]> ("Launchers", value.ToArray ());
			}
		}
		
		IEnumerable<string> Plugins {
			get {
				return GetOption<string[]> ("Plugins", new string[0]).AsEnumerable ();
			}
			set {
				SetOption<string[]> ("Plugins", value.ToArray ());
			}
		}
		
		bool FirstRun {
			get { return prefs.Get<bool> ("FirstRun", true); }
			set { prefs.Set<bool> ("FirstRun", value); }
		}
		
		public void ResetPreferences ()
		{
			SetOption<string> ("Autohide", "None");
			SetOption<bool> ("FadeOnHide", false);
			SetOption<double> ("FadeOpacity", 0);
			SetOption<int?> ("IconSize", 48);
			SetOption<bool?> ("IndicateMultipleWindows", false);
			SetOption<string[]> ("Launchers", new string[0]);
			SetOption<int?> ("MonitorNumber", 0);
			SetOption<string[]> ("Plugins", new string[0]);
			SetOption<string[]> ("SortList", new string[0]);
			SetOption<bool?> ("ThreeDimensional", false);
			SetOption<bool?> ("WindowManager", false);
			SetOption<bool?> ("ZoomEnabled", true);
			SetOption<double?> ("ZoomPercent", 2.0);
		}
		
		public DockPreferences (string dockName, int monitorNumber) : this(dockName)
		{
			MonitorNumber = monitorNumber;
		}
		
		public DockPreferences (string dockName)
		{
			prefs = DockServices.Preferences.Get<DockPreferences> ();
			
			// ensures position actually gets set
			position = (DockPosition) 100;
			
			this.Build ();
			
			// Manually set the tooltips <shakes fist at MD...>
			multiple_window_indicator_check.TooltipMarkup = Mono.Unix.Catalog.GetString (
			    "Causes launchers which currently manage more than one window to have an extra indicator under it.");
			window_manager_check.TooltipMarkup = Mono.Unix.Catalog.GetString (
			    "When set, windows which do not already have launchers on a dock will be added to this dock.");
			
			icon_scale.Adjustment.SetBounds (24, 129, 1, 1, 1);
			zoom_scale.Adjustment.SetBounds (1, 4.01, .01, .01, .01);
			
			zoom_scale.FormatValue += delegate(object o, FormatValueArgs args) {
				args.RetVal = string.Format ("{0:#}%", args.Value * 100);
			};
			
			name = dockName;
			
			BuildItemProviders ();
			BuildOptions ();
			
			icon_scale.ValueChanged += IconScaleValueChanged;
			zoom_scale.ValueChanged += ZoomScaleValueChanged;
			zoom_checkbutton.Toggled += ZoomCheckbuttonToggled;
			autohide_box.Changed += AutohideBoxChanged;
			fade_on_hide_check.Toggled += FadeOnHideToggled;
			
			DefaultProvider.ItemsChanged += HandleDefaultProviderItemsChanged;
			
			ShowAll ();
		}

		void HandleDefaultProviderItemsChanged (object sender, ItemsChangedArgs e)
		{
			Launchers = DefaultProvider.Uris;
		}
		
		void AutohideBoxChanged (object sender, EventArgs e)
		{
			Autohide = (AutohideType) autohide_box.Active;
			
			if (autohide_box.Active != (int) Autohide)
				autohide_box.Active = (int) Autohide;

			fade_on_hide_check.Sensitive = (int) Autohide > 0;
		}

		void FadeOnHideToggled (object sender, EventArgs e)
		{
			FadeOnHide = fade_on_hide_check.Active;
		}

		void ZoomCheckbuttonToggled (object sender, EventArgs e)
		{
			ZoomEnabled = zoom_checkbutton.Active;
			
			// may seem odd but its just a check
			zoom_checkbutton.Active = ZoomEnabled;
			zoom_scale.Sensitive = ZoomEnabled;
		}

		void ZoomScaleValueChanged (object sender, EventArgs e)
		{
			ZoomPercent = zoom_scale.Value;
			
			if (ZoomPercent != zoom_scale.Value)
				zoom_scale.Value = ZoomPercent;
		}

		void IconScaleValueChanged (object sender, EventArgs e)
		{
			IconSize = (int) icon_scale.Value;
			
			if (IconSize != icon_scale.Value)
				icon_scale.Value = IconSize;
		}
		
		public bool SetName (string name)
		{
			
			return false;
		}
		
		public string GetName ()
		{
			return name;
		}
		
		public void RemoveProvider (AbstractDockItemProvider provider)
		{
			item_providers.Remove (provider);
			
			OnItemProvidersChanged (null, provider.AsSingle ());
			SyncPlugins ();
		}
		
		public void AddProvider (AbstractDockItemProvider provider)
		{
			item_providers.Add (provider);
			provider.AddedToDock ();
			
			OnItemProvidersChanged (provider.AsSingle (), null);
			SyncPlugins ();
		}
		
		public bool ProviderCanMoveUp (AbstractDockItemProvider provider)
		{
			return provider != ItemProviders.Where (p => p != DefaultProvider).First ();
		}
		
		public bool ProviderCanMoveDown (AbstractDockItemProvider provider)
		{
			return provider != ItemProviders.Where (p => p != DefaultProvider).Last ();
		}
		
		public void MoveProviderUp (AbstractDockItemProvider provider)
		{
			int index = item_providers.IndexOf (provider);
			if (index < 1) return;
			
			AbstractDockItemProvider temp = item_providers [index - 1];
			item_providers [index - 1] = provider;
			item_providers [index] = temp;
			
			OnItemProvidersChanged (null, null);
			SyncPlugins ();
		}
		
		public void MoveProviderDown (AbstractDockItemProvider provider)
		{
			int index = item_providers.IndexOf (provider);
			if (index < 0 || index > item_providers.Count - 2) return;
			
			AbstractDockItemProvider temp = item_providers [index + 1];
			item_providers [index + 1] = provider;
			item_providers [index] = temp;
			
			OnItemProvidersChanged (null, null);
			SyncPlugins ();
		}
		
		public void SyncPreferences ()
		{
			UpdateSortList ();
		}
		
		void BuildOptions ()
		{
			try {
				Autohide = (AutohideType) Enum.Parse (typeof(AutohideType), 
													  GetOption ("Autohide", AutohideType.None.ToString ()));
			} catch {
				Autohide = AutohideType.None;
			}
			
			try {
				Position = (DockPosition) Enum.Parse (typeof(DockPosition), 
													   GetOption ("Position", DockPosition.Bottom.ToString ()));
			} catch {
				Position = DockPosition.Bottom;
			}
			
			if (WindowManager)
				DefaultProvider.SetWindowManager ();
			
			autohide_box.Active = (int) Autohide;
			UpdateAutohideDescription ();
			fade_on_hide_check.Sensitive = (int) Autohide > 0;
			
			panel_mode_button.Active = PanelMode;
			zoom_checkbutton.Active = ZoomEnabled;
			zoom_checkbutton.Sensitive = !PanelMode;
			zoom_scale.Value = ZoomPercent;
			zoom_scale.Sensitive = !PanelMode && ZoomEnabled;
			icon_scale.Value = IconSize;
			fade_on_hide_check.Active = FadeOnHide;
			threedee_check.Active = ThreeDimensional;
			threedee_check.Sensitive = Position == DockPosition.Bottom;
			multiple_window_indicator_check.Active = IndicateMultipleWindows;
			
			
			window_manager_check.Active = DefaultProvider.IsWindowManager;
			DefaultProvider.WindowManagerChanged += delegate {
				WindowManager = window_manager_check.Active = DefaultProvider.IsWindowManager;
			};
		}
		
		T GetOption<T> (string key, T def)
		{
			return prefs.Get<T> (name + "/" + key, def);
		}
		
		bool SetOption<T> (string key, T val)
		{
			return prefs.Set<T> (name + "/" + key, val);
		}
		
		void BuildItemProviders ()
		{
			item_providers = new List<AbstractDockItemProvider> ();
			
			DefaultProvider = new FileApplicationProvider ();
			item_providers.Add (DefaultProvider);
			
			if (FirstRun) {
				WindowManager = true;
				
				// TODO optimize this better, right now we try to find files and
				// pick the first one we find (which sorta works)
				// ideally, we should query the system for the 'default web browser'
				// etc and then use that
				
				// browser
				string launcher_browser = new[] {
					"file:///usr/share/applications/firefox.desktop",
					"file:///usr/share/applications/chromium-browser.desktop",
					"file:///usr/local/share/applications/google-chrome.desktop",
					"file:///usr/share/applications/epiphany.desktop",
					"file:///usr/share/applications/kde4/konqbrowser.desktop",
				}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath)).FirstOrDefault ();
				
				// terminal
				string launcher_terminal = new[] {
					"file:///usr/share/applications/terminator.desktop",
					"file:///usr/share/applications/gnome-terminal.desktop",
					"file:///usr/share/applications/kde4/konsole.desktop",
				}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath)).FirstOrDefault ();
				
				// music player
				string launcher_music = new[] {
					"file:///usr/share/applications/exaile.desktop",
					"file:///usr/share/applications/songbird.desktop",
					"file:///usr/share/applications/banshee-1.desktop",
					"file:///usr/share/applications/rhythmbox.desktop",
					"file:///usr/share/applications/kde4/amarok.desktop",
				}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath)).FirstOrDefault ();
				
				// IM client
				string launcher_im = new[] {
					"file:///usr/share/applications/pidgin.desktop",
					"file:///usr/share/applications/empathy.desktop",
				}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath)).FirstOrDefault ();
				
				Launchers = new[] {
					launcher_browser,
					launcher_terminal,
					launcher_music,
					launcher_im,
				}.Where (s => !String.IsNullOrEmpty (s));
				
				FirstRun = false;
			}
			
			foreach (string launcher in Launchers) {
				DefaultProvider.InsertItem (launcher);
			}
			
			// we have a plugin thats not enabled, go nuclear
			if (Plugins.Any (s => !PluginManager.ItemProviders.Any (ip => ip.Name == s))) {
				foreach (Mono.Addins.Addin addin in PluginManager.AllAddins) {
					addin.Enabled = true;
				}
			}
			
			foreach (string providerName in Plugins) {
				AbstractDockItemProvider provider = PluginManager.ItemProviders
					.Where (adip => adip.Name == providerName)
					.DefaultIfEmpty (null)
					.FirstOrDefault ();
				
				if (provider != null) {
					item_providers.Add (provider);
					provider.AddedToDock ();
				}
			}
			
			List<string> sortList = SortList.ToList ();
			foreach (AbstractDockItemProvider provider in item_providers) {
				SortProviderOnList (provider, sortList);
			}
			
			UpdateSortList ();
		}
		
		void SortProviderOnList (AbstractDockItemProvider provider, List<string> sortList)
		{
			int defaultRes = 1000;
			Func<AbstractDockItem, int> indexFunc = delegate(AbstractDockItem a) {
				int res = sortList.IndexOf (a.UniqueID ());
				return res >= 0 ? res : defaultRes++;
			};
			
			int i = 0;
			foreach (AbstractDockItem item in provider.Items.OrderBy (indexFunc)) {
				item.Position = i++;
			}
		}
		
		void UpdateSortList ()
		{
			SortList = item_providers
				.SelectMany (p => p.Items)
				.OrderBy (i => i.Position)
				.Select (i => i.UniqueID ());
		}
		
		void UpdateAutohideDescription ()
		{
			switch (autohide_box.Active) {
			case 0:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Never hides; maximized windows do not overlap the dock.</i>");
				break;
			case 1:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Hides whenever the mouse is not over it.</i>");
				break;
			case 2:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Hides when dock obstructs the active application.</i>");
				break;
			case 3:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Hides when dock obstructs any window.</i>");
				break;
			}
		}
		
		void OnAutohideChanged ()
		{
			UpdateAutohideDescription ();
			if (AutohideChanged != null)
				AutohideChanged (this, EventArgs.Empty);
		}
		
		void OnFadeOnHideChanged ()
		{
			if (FadeOnHideChanged != null)
				FadeOnHideChanged (this, EventArgs.Empty);
		}
		
		void OnFadeOpacityChanged ()
		{
			if (FadeOpacityChanged != null)
				FadeOpacityChanged (this, EventArgs.Empty);
		}
		
		void OnPositionChanged ()
		{
			if (PositionChanged != null)
				PositionChanged (this, EventArgs.Empty);
		}
		
		void OnIconSizeChanged ()
		{
			if (IconSizeChanged != null)
				IconSizeChanged (this, EventArgs.Empty);
		}
		
		void OnIndicatorSettingChanged ()
		{
			if (IndicatorSettingChanged != null)
				IndicatorSettingChanged (this, EventArgs.Empty);
		}
		
		void OnPanelModeChanged ()
		{
			if (PanelModeChanged != null)
				PanelModeChanged (this, EventArgs.Empty);
		}
			
		void OnThreeDimensionalChanged ()
		{
			if (ThreeDimensionalChanged != null)
				ThreeDimensionalChanged (this, EventArgs.Empty);
		}
		
		void OnZoomEnabledChanged ()
		{
			if (ZoomEnabledChanged != null)
				ZoomEnabledChanged (this, EventArgs.Empty);
		}
		
		void OnZoomPercentChanged ()
		{
			if (ZoomPercentChanged != null)
				ZoomPercentChanged (this, EventArgs.Empty);
		}

		protected virtual void OnMultipleWindowIndicatorCheckToggled (object sender, System.EventArgs e)
		{
			IndicateMultipleWindows = multiple_window_indicator_check.Active;
			multiple_window_indicator_check.Active = IndicateMultipleWindows;
		}
		
		protected virtual void OnWindowManagerCheckToggled (object sender, System.EventArgs e)
		{
			if (window_manager_check.Active)
				DefaultProvider.SetWindowManager ();
			WindowManager = window_manager_check.Active = DefaultProvider.IsWindowManager;
		}
		
		protected virtual void OnPanelModeButtonToggled (object sender, System.EventArgs e)
		{
			PanelMode = panel_mode_button.Active;
			panel_mode_button.Active = PanelMode;
			zoom_scale.Sensitive = !PanelMode && ZoomEnabled;
			zoom_checkbutton.Sensitive = !PanelMode;
		}
		
		protected virtual void OnThreedeeCheckToggled (object sender, System.EventArgs e)
		{
			ThreeDimensional = threedee_check.Active;
			threedee_check.Active = ThreeDimensional;
		}

		void OnItemProvidersChanged (IEnumerable<AbstractDockItemProvider> addedProviders, IEnumerable<AbstractDockItemProvider> removedProviders)
		{
			if (ItemProvidersChanged != null) {
				ItemProvidersChanged (this, new ItemProvidersChangedEventArgs (addedProviders, removedProviders));
			}
		}
		
		void SyncPlugins ()
		{
			Plugins = ItemProviders.Where (p => p != DefaultProvider).Select (p => p.Name);
		}
		
		public void FreeProviders ()
		{
			OnItemProvidersChanged (null, item_providers);
			
			foreach (AbstractDockItemProvider adip in item_providers.Where (adip => adip != DefaultProvider)) {
				PluginManager.Disable (adip);
			}
			
			foreach (AbstractDockItemProvider provider in item_providers)
				provider.Dispose ();
			item_providers = new List<AbstractDockItemProvider> ();
			FileApplicationProvider.WindowManager.UpdateTransientItems ();
			
			SyncPlugins ();
		}
		
		public override void Dispose ()
		{
			SyncPlugins ();
			SyncPreferences ();
			UpdateSortList ();
			
			base.Dispose ();
		}
	}
}
