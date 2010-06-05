//  
//  Copyright (C) 2010 Robert Dyer
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
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text;

using Gdk;
using Gtk;

using Mono.Unix;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Tar;

using Docky.Items;
using Docky.Interface;
using Docky.Services;

namespace Docky
{
	internal class ThemeController
	{
		static readonly string DefaultTheme = "Classic";
		
		static IPreferences prefs;
		
		public static event EventHandler ThemeChanged;
		
		static IEnumerable<GLib.File> ThemeContainerFolders {
			get {
				return new [] {
					DockServices.Paths.SystemDataFolder.GetChild ("themes"),
					DockServices.Paths.UserDataFolder.GetChild ("themes"),
				}.Where (d => d.Exists).Distinct (new FileEqualityComparer ());
			}
		}
		
		public static IEnumerable<string> DockThemes {
			get {
				yield return DefaultTheme;
				
				foreach (GLib.File dir in ThemeContainerFolders.Where (f => f.Exists))
					foreach (string s in Directory.GetDirectories (dir.Path))
						yield return Path.GetFileName (s);
			}
		}
		
		public static int UrgentHueShift {
			get {
				return prefs.Get<int> ("UrgentHue", 150);
			}
			set {
				if (UrgentHueShift == value)
					return;
				// clamp to -180 .. 180
				int hue = Math.Max (-180, Math.Min (180, value));
				prefs.Set ("UrgentHue", hue);
			}
		}
		
		static TimeSpan glow;
		static int? glowSeconds;
		public static TimeSpan GlowTime {
			get {
				if (!glowSeconds.HasValue) {
					glowSeconds = prefs.Get<int> ("GlowTime", 10);
					if (glowSeconds.Value < 0)
						glow = new TimeSpan (100, 0, 0, 0, 0);
					else
						glow = new TimeSpan (0, 0, 0, 0, glowSeconds.Value * 1000);
				}
				return glow;
			}
		}
		
		static string theme;
		public static string DockTheme {
			get { return theme; }
			set {
				if (theme == value)
					return;
				
				theme = value;
				prefs.Set ("Theme", theme);
				
				Log<ThemeController>.Info ("Setting theme: " + value);
				BackgroundSvg = ThemedSvg ("background.svg");
				Background3dSvg = ThemedSvg ("background3d.svg");
				if (Background3dSvg.IndexOf ("@") != -1)
					Background3dSvg = BackgroundSvg;
				MenuSvg = ThemedSvg ("menu.svg");
				TooltipSvg = ThemedSvg ("tooltip.svg");
				
				if (ThemeChanged != null)
					ThemeChanged (null, EventArgs.Empty);
			}
		}
		
		public static string BackgroundSvg { get; protected set; }
		
		public static string Background3dSvg { get; protected set; }
		
		public static string MenuSvg { get; protected set; }
		
		public static string TooltipSvg { get; protected set; }
		
		public static void Initialize ()
		{
			prefs = DockServices.Preferences.Get<ThemeController> ();
			
			DockTheme = prefs.Get ("Theme", DefaultTheme);
		}

		static string ThemedSvg (string svgName)
		{
			if (DockTheme != DefaultTheme) {
				GLib.File themeFolder = ThemeContainerFolders
					.SelectMany (f => f.SubDirs (false))
					.FirstOrDefault (th => th.Basename == DockTheme);
				
				if (themeFolder != null && themeFolder.GetChild (svgName).Exists)
					return themeFolder.GetChild (svgName).Path;
			}
			
			return svgName + "@" + System.Reflection.Assembly.GetExecutingAssembly ().FullName;
		}
		
		public static string InstallTheme (GLib.File file)
		{
			if (!file.Exists)
				return null;
			
			if (!DockServices.Paths.UserDataFolder.Exists)
				DockServices.Paths.UserDataFolder.MakeDirectory (null);
			
			GLib.File themeDir = DockServices.Paths.UserDataFolder.GetChild ("themes");
			if (!themeDir.Exists)
				themeDir.MakeDirectory (null);
			
			Log<ThemeController>.Info ("Trying to install theme: {0}", file.Path);
			
			try {
				List<string> oldThemes = DockThemes.ToList ();
				TarArchive ar = TarArchive.CreateInputTarArchive (new System.IO.FileStream (file.Path, System.IO.FileMode.Open));
				ar.ExtractContents (themeDir.Path);
				List<string> newThemes = DockThemes.ToList ();
				newThemes.RemoveAll (f => oldThemes.Contains (f));
				if (newThemes.Count == 1)
					return newThemes [0];
				return null;
			} catch (Exception e) {
				Log<ThemeController>.Error ("Error trying to unpack '{0}': {1}", file.Path, e.Message);
				Log<ThemeController>.Debug (e.StackTrace);
				return null;
			}
		}
	}
}
