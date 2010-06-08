//  
//  Copyright (C) 2009 Jason Smith, Chris Szikszoy
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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mono.Addins;
//using Mono.Addins.Gui;
//using Mono.Addins.Setup;

using Docky.Items;
using Docky.Services;
using Docky.Widgets;

namespace Docky
{
	public class AddinStateChangedEventArgs : EventArgs
	{
		public Addin Addin { get; private set; }
		public bool State { get; private set; }
		
		public AddinStateChangedEventArgs (Addin addin, bool state)
		{
			Addin = addin;
			State = state;
		}
	}
	
	public class PluginManager
	{
		public static readonly string DefaultPluginIcon = "package";
		
		const string IPExtensionPath = "/Docky/ItemProvider";
		const string ConfigExtensionPath = "/Docky/Configuration";
		
		public static event EventHandler<AddinStateChangedEventArgs> AddinStateChanged;

		//// <value>
		/// Directory where Docky saves its Mono.Addins repository cache.
		/// </value>
		public static GLib.File UserPluginsDirectory {
			get { return DockServices.Paths.UserDataFolder.GetChild ("plugins"); }
		}
		
		public static GLib.File UserAddinInstallationDirectory {
			get { return UserPluginsDirectory.GetChild ("addins"); }
		}
			
		/// <summary>
		/// Performs plugin system initialization. Should be called before this
		/// class or any Mono.Addins class is used. The ordering is very delicate.
		/// </summary>
		public static void Initialize ()
		{
			// Initialize Mono.Addins.
			try {
				AddinManager.Initialize (UserPluginsDirectory.Path);
			} catch (InvalidOperationException e) {
				Log<PluginManager>.Error ("AddinManager.Initialize: {0}", e.Message);
				Log<PluginManager>.Warn ("Rebuild Addin.Registry and reinitialize AddinManager");
				AddinManager.Registry.Rebuild (null);
				AddinManager.Shutdown ();
				AddinManager.Initialize (UserPluginsDirectory.Path);
			}

			AddinManager.Registry.Update (null);
			
			// Add feedback when addin is loaded or unloaded
			AddinManager.AddinLoaded += AddinManagerAddinLoaded;
			AddinManager.AddinUnloaded += AddinManagerAddinUnloaded;
		}
		
		public static void Shutdown ()
		{
			AddinManager.Shutdown ();
		}
		
		static void OnStateChanged (Addin addin, bool enabled)
		{
			if (AddinStateChanged != null)
				AddinStateChanged (null, new AddinStateChangedEventArgs (addin, enabled));
		}

		static void AddinManagerAddinLoaded (object sender, AddinEventArgs args)
		{
			Addin addin = AddinFromID (args.AddinId);
			OnStateChanged (addin, true);
			Log<PluginManager>.Info ("Loaded \"{0}\".", addin.Name);
		}

		static void AddinManagerAddinUnloaded (object sender, AddinEventArgs args)
		{
			Addin addin = AddinFromID (args.AddinId);
			OnStateChanged (addin, false);
			Log<PluginManager>.Info ("Unloaded \"{0}\".", addin.Name);
		}
		
		public static Addin AddinFromID (string id)
		{
			return AddinManager.Registry.GetAddin (id);
		}
		
		public static AbstractDockItemProvider Enable (Addin addin)
		{
			addin.Enabled = true;
			return ItemProviderFromAddin (addin.Id);
		}
		
		public static AbstractDockItemProvider Enable (string id)
		{
			return Enable (AddinFromID (id));
		}
		
		public static void Disable (Addin addin)
		{
			addin.Enabled = false;
		}
		
		public static void Disable (string id)
		{
			Disable (AddinFromID (id));
		}
		
		public static void Disable (AbstractDockItemProvider provider)
		{
			Disable (AddinIDFromProvider (provider));
		}
		
		public static IEnumerable<Addin> AllAddins {
			get {
				return AddinManager.Registry.GetAddins ();
			}
		}
		
		public static void InstallLocalPlugins ()
		{	
			IEnumerable<string> manual;
			
			manual = UserAddinInstallationDirectory.GetFiles ("*.dll").Select (f => f.Basename);
					
			manual.ToList ().ForEach (dll => Log<PluginManager>.Info ("Installing {0}", dll));
			
			AddinManager.Registry.Rebuild (null);
				
			manual.ToList ().ForEach (dll => File.Delete (dll));
		}
		
		static T ObjectFromAddin<T> (string extensionPath, string addinID) where T : class
		{
			IEnumerable<TypeExtensionNode> nodes = AddinManager.GetExtensionNodes (extensionPath)
				.OfType<TypeExtensionNode> ()
				.Where (a => Addin.GetIdName (a.Addin.Id) == Addin.GetIdName (addinID));
			
			if (nodes.Any ())
				return nodes.First ().GetInstance () as T;
			return null;
		}
		
		public static AbstractDockItemProvider ItemProviderFromAddin (string addinID)
		{
			return ObjectFromAddin<AbstractDockItemProvider> (IPExtensionPath, addinID);
		}

		public static ConfigDialog ConfigForAddin (string addinID)
		{
			return  ObjectFromAddin<ConfigDialog> (ConfigExtensionPath, addinID);
		}
		
		public static string AddinIDFromProvider (AbstractDockItemProvider provider)
		{
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (IPExtensionPath)) {
				AbstractDockItemProvider nodeProvider;
				
				try {
					nodeProvider = node.GetInstance () as AbstractDockItemProvider;
				} catch {
					continue;
				}
				
				if (nodeProvider.Name == provider.Name)
					return node.Addin.Id;
			}
			
			// shouldn't happen
			return "";
		}

		/// <value>
		/// All loaded ItemProviders.
		/// </value>
		public static IEnumerable<AbstractDockItemProvider> ItemProviders {
			get { return AddinManager.GetExtensionObjects (IPExtensionPath).OfType<AbstractDockItemProvider> (); }
		}
		
		// this will return a list of Provider IDs that are currently not used by any docks
		public static IEnumerable<string> AvailableProviderIDs {
			get {
				return AllAddins .Where (a => !a.Enabled).Select (a => Addin.GetIdName (a.Id));
			}
		}
	}
}
