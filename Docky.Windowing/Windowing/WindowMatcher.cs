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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Wnck;

namespace Docky.Windowing
{


	public class WindowMatcher
	{
		static WindowMatcher def;
		public static WindowMatcher Default {
			get {
				if (def == null)
					def = new WindowMatcher ();
				return def;
			}
		}
		
		static IEnumerable<string> DesktopFiles {
			get {
				return DesktopFileDirectories
					.SelectMany (dir => Directory.GetFiles (dir, "*.desktop"));
			}
		}
		
		static IEnumerable<string> DesktopFileDirectories
		{
			get {
				return new [] {
					// These are XDG variables...
					"XDG_DATA_HOME",
					"XDG_DATA_DIRS"
				}.SelectMany (v => XdgEnvironmentPaths (v))
				 .Where (d => Directory.Exists (d));
			}
		}
		
		static IEnumerable<string> XdgEnvironmentPaths (string xdgVar)
		{
			string envPath = Environment.GetEnvironmentVariable (xdgVar);
			
			if (string.IsNullOrEmpty (envPath)) {
				switch (xdgVar) {
				case "XDG_DATA_HOME":
					yield return Path.Combine (
						Environment.GetFolderPath (Environment.SpecialFolder.Personal),
						".local/share/applications"
					);
					break;
				case "XDG_DATA_DIRS":
					yield return "/usr/local/share/applications";
					yield return "/usr/share/applications";
					break;
				}
			} else {
				foreach (string dir in envPath.Split (':')) {
					yield return Path.Combine (dir, "applications");
				}
			}
		}
		
		static IEnumerable<string> PrefixStrings {
			get {
				yield return "gksu";
				yield return "sudo";
				yield return "java";
				yield return "mono";
				yield return "ruby";
				yield return "padsp";
				yield return "aoss";
				yield return "python(\\d.\\d)?";
				yield return "(ba)?sh";
			}
		}
		
		Dictionary<Wnck.Window, string> window_to_desktop_file;
		Dictionary<string, string> desktop_file_exec_strings;
		List<Regex> prefix_filters;
		List<Wnck.Window> windows;
		
		WindowMatcher ()
		{
			prefix_filters = BuildPrefixFilters ();
			desktop_file_exec_strings = BuildExecStrings ();
			windows = Wnck.Screen.Default.Windows.ToList ();
			
			window_to_desktop_file = new Dictionary<Wnck.Window, string> ();
			foreach (Wnck.Window w in windows) {
				SetupWindow (w);
			}
			
			Wnck.Screen.Default.WindowOpened += WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed += WnckScreenDefaultWindowClosed;
		}

		#region Window Setup
		void WnckScreenDefaultWindowOpened (object o, WindowOpenedArgs args)
		{
			windows = Wnck.Screen.Default.Windows.ToList ();
			SetupWindow (args.Window);
		}

		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			windows = Wnck.Screen.Default.Windows.ToList ();
			if (args.Window != null)
				window_to_desktop_file.Remove (args.Window);
		}
		
		void SetupWindow (Wnck.Window window)
		{
			window_to_desktop_file[window] = FindDesktopFileForWindowOrDefault (window);
			
			window.NameChanged += delegate {
				window_to_desktop_file[window] = FindDesktopFileForWindowOrDefault (window);
			};
		}
		#endregion
		
		public IEnumerable<Wnck.Window> WindowsForDesktopFile (string desktop_file)
		{
			foreach (KeyValuePair<Wnck.Window, string> kvp in window_to_desktop_file) {
				if (kvp.Value == desktop_file)
					yield return kvp.Key;
			}
		}
		
		public IEnumerable<Wnck.Window> SimilarWindows (Wnck.Window window)
		{
			if (!window_to_desktop_file.ContainsKey (window))
				return new[] { window };
			string id = window_to_desktop_file[window];
			
			return window_to_desktop_file.Where (kvp => kvp.Value == id).Select (kvp => kvp.Key);
		}
		
		public string DesktopFileForWindow (Wnck.Window window)
		{
			string file = window_to_desktop_file[window];
			return (file.EndsWith (".desktop")) ? file : null;
		}
		
		string FindDesktopFileForWindowOrDefault (Wnck.Window window)
		{
			int pid = window.Pid;
			if (pid <= 1)
				return window.Name;
			
			string[] command_line = CommandLineForPid (pid);
			
			// if we have a classname that matches a desktopid we have a winner
			if (window.ClassGroup != null) {
				string class_name = window.ClassGroup.ResClass;
				
				IEnumerable<string> matches = DesktopFiles
					.Where (file => Path.GetFileNameWithoutExtension (file).Equals (class_name, StringComparison.CurrentCultureIgnoreCase));
				
				if (matches.Any ())
					return matches.First ();
			}
			
			if (desktop_file_exec_strings.ContainsKey (command_line[0])) {
				return desktop_file_exec_strings[command_line[0]];
			}
			
			return window.Pid.ToString ();
		}
			
		string[] CommandLineForPid (int pid)
		{
			string cmdline;
			
			try {
				string procPath = new [] { "/proc", pid.ToString (), "cmdline" }.Aggregate (Path.Combine);
				using (StreamReader reader = new StreamReader (procPath)) {
					cmdline = reader.ReadLine ();
					reader.Close ();
				}
			} catch { return null; }
			
			string[] result = cmdline.Split (Convert.ToChar (0x0));
			
			return result
				.Where (s => !prefix_filters.Any (f => f.IsMatch (s)))
				.ToArray ();
		}
		
		Dictionary<string, string> BuildExecStrings ()
		{
			Dictionary<string, string> result = new Dictionary<string, string> ();
			
			foreach (string file in DesktopFiles) {
				Gnome.DesktopItem item = Gnome.DesktopItem.NewFromFile (file, 0);
				if (item == null || !item.AttrExists ("Exec"))
					continue;
				
				string exec = item.GetString ("Exec");
				
				string[] parts = exec.Split (' ');
				
				exec = parts
					.DefaultIfEmpty (null)
					.Where (part => !prefix_filters.Any (f => f.IsMatch (part)))
					.FirstOrDefault ();
				
				if (exec == null)
					continue;
				
				result [exec] = file;
				item.Dispose ();
			}
			
			return result;
		}
		
		List<Regex> BuildPrefixFilters ()
		{
			return new List<Regex> (PrefixStrings.Select (s => new Regex (s)));
		}
	}
}
