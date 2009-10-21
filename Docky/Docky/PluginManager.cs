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

using Mono.Addins;
using Mono.Addins.Gui;
using Mono.Addins.Setup;

using Docky.Items;
using Docky.Services;

namespace Docky
{


	public class PluginManager
	{
		const string DefaultPluginIcon = "folder_tar";
		
		const string PluginsDirectory = "plugins";
		const string ApplicationDirectory = "docky";
		
		const string IPExtensionPath = "/Docky/ItemProvider";

		//// <value>
		/// Directory where Docky saves its Mono.Addins repository cache.
		/// </value>
		static string UserPluginsDirectory {
			get {
				string userData = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
				return Path.Combine (Path.Combine (userData, ApplicationDirectory), PluginsDirectory);
			}
		}
			
		/// <summary>
		/// Performs plugin system initialization. Should be called before this
		/// class or any Mono.Addins class is used. The ordering is very delicate.
		/// </summary>
		public static void Initialize ()
		{
			// Initialize Mono.Addins.
			AddinManager.Initialize (UserPluginsDirectory);
			
			AddinManager.Registry.Rebuild (null);
			AddinManager.Registry.Update (null);
			
			foreach (Addin a in AddinManager.Registry.GetAddins ())
			{
				Console.WriteLine ("{0} is enabled? {1}", a.Name, a.Enabled);
				if (!a.Enabled)
					Enable (a);
			}
			
			/*
			// Get the extension nodes in the extension point
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Docky/ItemProvider")) {
				Enable (node.Id);
				object plugin = node.GetInstance ();
				Console.WriteLine (plugin.GetType ().Name);
			}
			*/
			
			AddinManager.AddExtensionNodeHandler (IPExtensionPath, OnPluginEvent);
		}
		
		static void OnPluginEvent (object sender, ExtensionNodeEventArgs args)
		{
			TypeExtensionNode node = args.ExtensionNode as TypeExtensionNode;
			
			switch (args.Change) {
			case ExtensionChange.Add:
				try {
					object plugin = node.GetInstance ();
					Log<PluginManager>.Debug ("Loaded \"{0}\" from plugin.", plugin.GetType ().Name);
				} catch (Exception e) {
					Log<PluginManager>.Error ("Encountered error loading plugin: {0} \"{1}\"",
							e.GetType ().Name, e.Message);
					Log<PluginManager>.Debug (e.StackTrace);
				}
				break;
			case ExtensionChange.Remove:
				try {
					object plugin = node.GetInstance ();
					Log<PluginManager>.Debug ("Unloaded \"{0}\".", plugin.GetType ().Name);
				} catch (Exception e) {
					Log<PluginManager>.Error ("Encountered error unloading plugin: {0} \"{1}\"",
							e.GetType ().Name, e.Message);
					Log<PluginManager>.Debug (e.StackTrace);
				}
				break;
			}	
		}
		
		public static void Enable (Addin addin)
		{
			SetAddinEnabled (addin, true);
		}
		
		public static void Enable (string id)
		{
			Enable (AddinManager.Registry.GetAddin (id));
		}
		
		public static void Disable (Addin addin)
		{
			SetAddinEnabled (addin, false);
		}
		
		public static void Disable (string id)
		{
			Disable (AddinManager.Registry.GetAddin (id));
		}
		
		static void SetAddinEnabled (Addin addin, bool enabled)
		{
			if (addin != null)
				addin.Enabled = enabled;
		}
		
		public static IEnumerable<Addin> GetAddins ()
		{
			return AddinManager.Registry.GetAddins ();
		}

		/// <value>
		/// All loaded ItemSources.
		/// </value>
		public static IEnumerable<AbstractDockItemProvider> ItemProviders {
			get { return AddinManager.GetExtensionObjects ("/Docky/ItemProvider").OfType<AbstractDockItemProvider> (); }
		}
	}
}
