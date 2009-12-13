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
		
		AddinTreeView inactive_view, active_view;
		
		public event EventHandler PositionChanged;
		public event EventHandler PanelModeChanged;
		public event EventHandler IconSizeChanged;
		public event EventHandler AutohideChanged;
		public event EventHandler FadeOnHideChanged;
		public event EventHandler FadeOpacityChanged;
		public event EventHandler IndicatorSettingChanged;
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
		
			
			// TreeViews
			inactive_view = new AddinTreeView (false);
			inactive_view.ButtonPressEvent += OnInactiveViewDoubleClicked;
			inactive_scroll.Add (inactive_view);
			
			active_view = new AddinTreeView (true);
			active_view.ButtonPressEvent += OnActiveViewDoubleClicked;
			active_view.AddinOrderChanged += OnActiveViewAddinOrderChanged;
			active_scroll.Add (active_view);
			
			// drag + drop for addin install
			TargetEntry addin = new TargetEntry ("text/uri-list", 0, 0);

			inactive_view.EnableModelDragDest (new [] {addin}, DragAction.Copy);
			inactive_view.DragDataReceived += HandleInactiveViewDragDataReceived;
			
			// more or less happens every time the visiblity of the widget changes.
			// kind of a dirty hack, good refactoring candidate
			Mapped += delegate {
				PopulateTreeViews ();
			};
			
			Shown += delegate {
				PopulateTreeViews ();
			};
			
			DefaultProvider.ItemsChanged += HandleDefaultProviderItemsChanged;
			
			ShowAll ();
	
		}

		void HandleDefaultProviderItemsChanged (object sender, ItemsChangedArgs e)
		{
			Launchers = DefaultProvider.Uris;
		}

		void HandleInactiveViewDragDataReceived (object o, DragDataReceivedArgs args)
		{
			string data;
			
			data = Encoding.UTF8.GetString (args.SelectionData.Data);
			// Sometimes we get a null at the end, and it crashes us.
			data = data.TrimEnd ('\0');
			
			foreach (string uri in data.Split (new [] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)) {
				string file, path, filename;
				
				if (string.IsNullOrEmpty (uri))
					continue;
				
				file = uri.Remove (0, 7); // 7 is the length of file://
				filename = System.IO.Path.GetFileName (file);
				
				if (!file.EndsWith (".dll"))
					continue;
				
				try {
					if (!Directory.Exists (PluginManager.UserAddinInstallationDirectory))
						Directory.CreateDirectory (PluginManager.UserAddinInstallationDirectory);
					
					path = System.IO.Path.Combine (PluginManager.UserAddinInstallationDirectory, filename);
					File.Copy (file, path, true);
					
				} catch (Exception e) {
					Log.Error ("{0} failed to process '{1}': {2}", name, uri, e.Message);
					Log.Info (e.StackTrace);
				}
			}
			
			PluginManager.InstallLocalPlugins ();
			PopulateTreeViews ();
		}
		
		void PopulateTreeViews ()
		{
			active_view.Clear ();
			inactive_view.Clear ();

			foreach (string id in PluginManager.AvailableProviderIDs)
				inactive_view.Add (new AddinTreeNode (id));
			
			foreach (AbstractDockItemProvider provider in ItemProviders.Where (p => p != DefaultProvider))
				active_view.Add (new AddinTreeNode (provider));
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
		
		public void DisableDocklet (AbstractDockItemProvider provider)
		{
			PluginManager.Disable (provider);
			item_providers.Remove (provider);
			
			OnItemProvidersChanged (null, provider.AsSingle ());
			SyncPlugins ();
			
			PopulateTreeViews ();
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
				Autohide = (AutohideType) Enum.Parse (typeof(AutohideType), AutohideType.None.ToString ());
			}
			
			try {
				Position = (DockPosition) Enum.Parse (typeof(DockPosition), 
																   GetOption ("Position", DockPosition.Bottom.ToString ()));
			} catch {
				Position = (DockPosition) Enum.Parse (typeof(DockPosition), DockPosition.Bottom.ToString ());
			}
			
			if (WindowManager)
				DefaultProvider.SetWindowManager ();
			
			autohide_box.Active = (int) Autohide;
			fade_on_hide_check.Sensitive = (int) Autohide > 0;
			
			panel_mode_button.Active = PanelMode;
			zoom_checkbutton.Active = ZoomEnabled;
			zoom_checkbutton.Sensitive = !PanelMode;
			zoom_scale.Value = ZoomPercent;
			zoom_scale.Sensitive = !PanelMode && ZoomEnabled;
			icon_scale.Value = IconSize;
			fade_on_hide_check.Active = FadeOnHide;
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
				
				Launchers = new[] {
					"file:///usr/share/applications/banshee-1.desktop",
					"file:///usr/share/applications/gnome-terminal.desktop",
					"file:///usr/share/applications/pidgin.desktop",
					"file:///usr/share/applications/xchat.desktop",
					"file:///usr/share/applications/firefox.desktop",
				}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath));
				
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
		
		void OnAutohideChanged ()
		{
			switch (autohide_box.Active) {
			case 0:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Always present, acts like a panel.</i>");
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
		
		void OnActiveViewAddinOrderChanged (object sender, AddinOrderChangedArgs e)
		{
			item_providers = item_providers
				.OrderBy (adip => e.NewOrder
					.Select (a => a.Provider)
					.ToList ()
					.IndexOf (adip))
				.ToList ();
			
			SyncPlugins ();	
			OnItemProvidersChanged (null, null);
		}

		[GLib.ConnectBefore]
		protected virtual void OnActiveViewDoubleClicked (object sender, ButtonPressEventArgs e)
		{
			if (e.Event.Type == EventType.TwoButtonPress)
				OnDisablePluginButtonClicked (sender, e);
		}

		protected virtual void OnDisablePluginButtonClicked (object sender, System.EventArgs e)
		{
			if (inactive_view.SelectedAddin != null)
				return;
			
			AddinTreeNode node = active_view.SelectedAddin as AddinTreeNode;
			
			if (node == null)
				return;
			
			DisableAddin (node);
		}
		
		private void DisableAddin (AddinTreeNode node)
		{
			// disable this addin
			PluginManager.Disable (node.AddinID);
			
			// remove it from the active addins list
			active_view.Remove (node);
			
			// remove it from the dock
			item_providers.Remove (node.Provider);
			OnItemProvidersChanged (null, node.Provider.AsSingle ());
			
			node.Provider = null;
			
			// add it back to the list of available addins
			inactive_view.Add (node);

			SyncPlugins ();
		}		
		
		[GLib.ConnectBefore]
		protected virtual void OnInactiveViewDoubleClicked (object sender, ButtonPressEventArgs e)
		{
			if (e.Event.Type == EventType.TwoButtonPress)
				OnEnablePluginButtonClicked (sender, e);
		}
		
		protected virtual void OnEnablePluginButtonClicked (object sender, System.EventArgs e)
		{
			if (inactive_view.SelectedAddin == null)
				return;
			
			AddinTreeNode node = inactive_view.SelectedAddin as AddinTreeNode;
			
			if (node == null)
				return;
			
			EnableAddin (node);
		}
		
		private void EnableAddin (AddinTreeNode node)
		{
			// enable the addin
			PluginManager.Enable (node.AddinID);
			
			// create the object
			AbstractDockItemProvider provider = PluginManager.ItemProviderFromAddin (node.AddinID);
			
			node.Provider = provider;
			
			if (provider == null) {
				Log<DockPreferences>.Warn ("Could not enable {0}: Item was null", node.Name);
				return;
			}
			
			// remove this addin from the inactive list
			inactive_view.Remove (node);
			
			// add this provider to the list of enabled providers and trigger ProvidersChanged
			item_providers.Add (provider);
			provider.AddedToDock ();
			OnItemProvidersChanged (provider.AsSingle (), null);
			
			// Add the node to the enabled providers treeview
			active_view.Add (node);
			
			SyncPlugins ();	
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
			item_providers = new List<AbstractDockItemProvider> ();
			
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
