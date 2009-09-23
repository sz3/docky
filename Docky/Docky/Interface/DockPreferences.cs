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

using Cairo;
using Gdk;
using Gtk;

using Docky.Items;
using Docky.Services;

namespace Docky.Interface
{


	[System.ComponentModel.ToolboxItem(true)]
	public partial class DockPreferences : Gtk.Bin, IDockPreferences
	{
		static T Clamp<T> (T value, T max, T min)
		where T : IComparable<T> {     
			T result = value;
			if (value.CompareTo (max) > 0)
				result = max;
			if (value.CompareTo (min) < 0)
				result = min;
			return result;
		} 
		
		IPreferences prefs;
		string name;
		List<IDockItemProvider> item_providers;
		
		public event EventHandler PositionChanged;
		public event EventHandler IconSizeChanged;
		public event EventHandler AutohideChanged;
		public event EventHandler FadeOnHideChanged;
		public event EventHandler FadeOpacityChanged;
		public event EventHandler ZoomEnabledChanged;
		public event EventHandler ZoomPercentChanged;
		
		public event EventHandler<ItemProvidersChangedEventArgs> ItemProvidersChanged;
		
		public FileApplicationProvider DefaultProvider { get; set; }
		
		#region Public Properties
		public IEnumerable<string> SortList {
			get { return GetOption<string[]> ("SortList", new string[0]); }
			set { SetOption<string[]> ("SortList", value.ToArray ()); }
		}
		
		public IEnumerable<IDockItemProvider> ItemProviders { 
			get { return item_providers.AsEnumerable (); }
		}
		
		AutohideType hide_type;
		public AutohideType Autohide {
			get { return hide_type; }
			set {
				if (hide_type == value)
					return;
				hide_type = value;
				SetOption ("Autohide", hide_type.ToString ());
				OnAutohideChanged ();
			}
		}
		
		bool? fade_on_hide;
		public bool FadeOnHide {
			get {
				if (!fade_on_hide.HasValue)
					fade_on_hide = GetOption ("FadeOnHide", false);
				return fade_on_hide.Value; 
			}
			set {
				if (fade_on_hide == value)
					return;
				fade_on_hide = value;
				SetOption ("FadeOnHide", fade_on_hide.Value);
				OnFadeOnHideChanged ();
			}
		}
		
		double? fade_opacity;
		public double FadeOpacity {
			get {
				if (!fade_opacity.HasValue)
					fade_opacity = GetOption ("FadeOpacity", 0.4);
				return fade_opacity.Value; 
			}
			set {
				if (fade_opacity == value)
					return;
				fade_opacity = value;
				SetOption ("FadeOpacity", fade_opacity.Value);
				OnFadeOpacityChanged ();
			}
		}
		
		DockPosition position;
		public DockPosition Position {
			get { return position; }
			set {
				if (position == value || Docky.Controller.Docks.Any (d => d.Preferences.Position == value))
					return;
				position = value;
				SetOption ("Position", position.ToString ());
				OnPositionChanged ();
			}
		}
		
		int? icon_size;
		public int IconSize {
			get {
				if (!icon_size.HasValue) {
					icon_size = GetOption ("IconSize", 64);
				}
				return icon_size.Value; 
			}
			set {
				value = Clamp (value, 128, 24);
				if (icon_size == value)
					return;
				icon_size = value;
				SetOption ("IconSize", icon_size.Value);
				OnIconSizeChanged ();
			}
		}
		
		bool? zoom_enabled;
		public bool ZoomEnabled {
			get {
				if (!zoom_enabled.HasValue)
					zoom_enabled = GetOption ("ZoomEnabled", true);
				return zoom_enabled.Value; 
			}
			set {
				if (zoom_enabled == value)
					return;
				zoom_enabled = value;
				SetOption ("ZoomEnabled", zoom_enabled.Value);
				OnZoomEnabledChanged ();
			}
		}
				
		double? zoom_percent;
		public double ZoomPercent {
			get {
				if (!zoom_percent.HasValue)
					zoom_percent = GetOption ("ZoomPercent", 2.0);
				return zoom_percent.Value; 
			}
			set {
				value = Clamp (value, 4, 1);
				if (zoom_percent == value)
					return;
				
				zoom_percent = value;
				SetOption<double> ("ZoomPercent", zoom_percent.Value);
				OnZoomPercentChanged ();
			}
		}
		#endregion
		
		bool? window_manager;
		public bool WindowManager {
			get {
				if (!window_manager.HasValue)
					window_manager = GetOption ("WindowManager", false);
				return window_manager.Value; 
			}
			set {
				window_manager = value;
				SetOption ("WindowManager", window_manager);
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
		
		bool FirstRun {
			get { return prefs.Get ("FirstRun", true); }
			set { prefs.Set ("FirstRun", value); }
		}
		
		public DockPreferences (string dockName)
		{
			// ensures position actually gets set
			position = (DockPosition) 100;
			
			this.Build ();
			
			icon_scale.Adjustment.SetBounds (24, 129, 1, 1, 1);
			zoom_scale.Adjustment.SetBounds (1, 4.01, .01, .01, .01);
			
			zoom_scale.FormatValue += delegate(object o, FormatValueArgs args) {
				args.RetVal = string.Format ("{0:#}%", args.Value * 100);
			};
			
			name = dockName;
			
			prefs = DockServices.Preferences.Get<DockPreferences> ();
			
			BuildItemProviders ();
			BuildOptions ();
			
			icon_scale.ValueChanged += IconScaleValueChanged;
			zoom_scale.ValueChanged += ZoomScaleValueChanged;
			zoom_checkbutton.Toggled += ZoomCheckbuttonToggled;
			autohide_box.Changed += AutohideBoxChanged;
			position_box.Changed += PositionBoxChanged;
			fade_on_hide_check.Toggled += FadeOnHideToggled;
			
			ShowAll ();
		}

		void PositionBoxChanged (object sender, EventArgs e)
		{
			Position = (DockPosition) position_box.Active;
			
			if (position_box.Active != (int) Position)
				position_box.Active = (int) Position;
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
		
		public void SyncPreferences ()
		{
			UpdateSortList ();
		}
		
		void BuildOptions ()
		{
			Autohide = (AutohideType) Enum.Parse (typeof(AutohideType), 
			                                      GetOption ("Autohide", AutohideType.None.ToString ()));
			
			DockPosition position = (DockPosition) Enum.Parse (typeof(DockPosition), 
			                                                   GetOption ("Position", DockPosition.Bottom.ToString ()));
			
			while (Docky.Controller.Docks.Any ((Dock d) => d.Preferences.Position == position)) {
				Console.Error.WriteLine ("Dock position already in use: " + position.ToString ());
				position = (DockPosition) ((((int) position) + 1) % 4);
			}
			Position = position;
			
			
			if (WindowManager)
				DefaultProvider.SetWindowManager ();
			
			position_box.Active = (int) Position;
			autohide_box.Active = (int) Autohide;
			fade_on_hide_check.Sensitive = (int) Autohide > 0;
			
			zoom_checkbutton.Active = ZoomEnabled;
			zoom_scale.Value = ZoomPercent;
			zoom_scale.Sensitive = ZoomEnabled;
			icon_scale.Value = IconSize;
			fade_on_hide_check.Active = FadeOnHide;
			
			
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
			item_providers = new List<IDockItemProvider> ();
			
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
				};
				FirstRun = false;
			}
			
			foreach (string launcher in Launchers) {
				DefaultProvider.InsertItem (launcher);
			}
			
			DefaultProvider.ItemsChanged += DefaultProviderItemsChanged;
			
			List<string> sortList = SortList.ToList ();
			foreach (IDockItemProvider provider in item_providers) {
				SortProviderOnList (provider, sortList);
			}
			
			UpdateSortList ();
		}

		void DefaultProviderItemsChanged (object sender, ItemsChangedArgs e)
		{
			Launchers = DefaultProvider.Uris;
			
			// remove item from sort list
			if (e.Type == AddRemoveChangeType.Remove && e.Item != null) {
				SortList = SortList.Where (s => s != e.Item.UniqueID ());
			}
			
			// by definition, we got here first since we hooked up this event before anyone else
			// could get ahold of this. therefor we get our event evoked first.
			SortProviderOnList (DefaultProvider, SortList.ToList ());
		}
		
		void SortProviderOnList (IDockItemProvider provider, List<string> sortList)
		{
			Func<AbstractDockItem, int> indexFunc = delegate(AbstractDockItem a) {
				int res = sortList.IndexOf (a.UniqueID ());
				return res >= 0 ? res : 1000;
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

		protected virtual void OnWindowManagerCheckToggled (object sender, System.EventArgs e)
		{
			if (window_manager_check.Active)
				DefaultProvider.SetWindowManager ();
			else
				DefaultProvider.UnsetWindowManager ();
		}
	}
}
