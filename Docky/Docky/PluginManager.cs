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
using Mono.Addins.Gui;
using Mono.Addins.Setup;

using Docky.Items;
using Docky.Services;

namespace Docky
{


	public class PluginManager
	{
		public static readonly string DefaultPluginIcon = "folder_tar";
		
		const string PluginsDirectory = "plugins";
		const string ApplicationDirectory = "docky";
		const string DefaultAddinsDirectory = "addins";
		
		const string IPExtensionPath = "/Docky/ItemProvider";

		//// <value>
		/// Directory where Docky saves its Mono.Addins repository cache.
		/// </value>
		public static string UserPluginsDirectory {
			get {
				string userData = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
				return Path.Combine (Path.Combine (userData, ApplicationDirectory), PluginsDirectory);
			}
		}
		
		public static string UserAddinInstallationDirectory {
			get { return Path.Combine (UserPluginsDirectory, DefaultAddinsDirectory); }
		}
			
		/// <summary>
		/// Performs plugin system initialization. Should be called before this
		/// class or any Mono.Addins class is used. The ordering is very delicate.
		/// </summary>
		public static void Initialize ()
		{
			// Initialize Mono.Addins.
			AddinManager.Initialize (UserPluginsDirectory);
			
			AddinManager.Registry.Update (null);
			
			// Add feedback when addin is loaded or unloaded
			AddinManager.AddinLoaded += AddinManagerAddinLoaded;
			AddinManager.AddinUnloaded += AddinManagerAddinUnloaded;
		}
		
		public static void Shutdown ()
		{
			AddinManager.Shutdown ();
		}

		static void AddinManagerAddinLoaded (object sender, AddinEventArgs args)
		{
			Addin addin = AddinFromID (args.AddinId);
			Log<PluginManager>.Info ("Loaded \"{0}\".", addin.Name);
		}

		static void AddinManagerAddinUnloaded (object sender, AddinEventArgs args)
		{
			Addin addin = AddinFromID (args.AddinId);
			Log<PluginManager>.Info ("Unloaded \"{0}\".", addin.Name);
		}
		
		public static Addin AddinFromID (string id)
		{
			return AddinManager.Registry.GetAddin (id);
		}
		
		public static void Enable (Addin addin)
		{
			SetAddinEnabled (addin, true);
		}
		
		public static void Enable (string id)
		{
			Enable (AddinFromID (id));
		}
		
		public static void Disable (Addin addin)
		{
			SetAddinEnabled (addin, false);
		}
		
		public static void Disable (string id)
		{
			Disable (AddinFromID (id));
		}
		
		static void SetAddinEnabled (Addin addin, bool enabled)
		{
			if (addin != null)
				addin.Enabled = enabled;
		}
		
		public static IEnumerable<Addin> AllAddins {
			get {
				return AddinManager.Registry.GetAddins ();
			}
		}
		
		public static void InstallLocalPlugins ()
		{	
			IEnumerable<string> saved, manual;
			
			manual = Directory.GetFiles (UserAddinInstallationDirectory, "*.dll")
				.Select (s => Path.GetFileName (s));
					
			manual.ToList ().ForEach (dll => Log<PluginManager>.Info ("Installing {0}", dll));
			
			AddinManager.Registry.Rebuild (null);
			
			saved = AllAddins
				.Where (addin => manual.Contains (Path.GetFileName (addin.AddinFile)))
				.Select (addin => addin.Id);
				
			manual.ToList ().ForEach (dll => File.Delete (dll));
		}
		
		public static AbstractDockItemProvider ItemProviderFromAddin (string addinID)
		{
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (IPExtensionPath)) {
				object provider;
				
				try {
					provider = node.GetInstance ();
				} catch (Exception) {
					continue;
				}
				
				if (Addin.GetIdName (addinID) == Addin.GetIdName (node.Addin.Id))
				    return provider as AbstractDockItemProvider;
			}
			
			// shouldn't happen
			return null;
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
