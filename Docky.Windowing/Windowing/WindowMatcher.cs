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
					.SelectMany (dir => FindDesktopFiles (dir));
			}
		}
		
		static IEnumerable<string> FindDesktopFiles (string dir) {
			IEnumerable<string> files;
			
			try {
				files = Directory.GetFiles (dir, "*.desktop");
			} catch {
				return Enumerable.Empty<string> ();
			}
			
			foreach (string subdir in Directory.GetDirectories (dir))
				files = files.Union (FindDesktopFiles (subdir));
			
			return files;
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
				yield return "-.*";
				yield return "*.\\.desktop";
			}
		}
		
		static IEnumerable<string> SuffixStrings {
			get {
				yield return "-bin";
			}
		}
		
		Dictionary<Wnck.Window, List<string>> window_to_desktop_files;
		Dictionary<string, List<string>> exec_to_desktop_files;
		List<Regex> prefix_filters;
		Wnck.Screen screen;
		
		WindowMatcher ()
		{
			screen = Wnck.Screen.Default;
			prefix_filters = BuildPrefixFilters ();
			exec_to_desktop_files = BuildExecStrings ();
			
			window_to_desktop_files = new Dictionary<Wnck.Window, List<string>> ();
			foreach (Wnck.Window w in screen.Windows) {
				SetupWindow (w);
			}
			
			screen.WindowOpened += WnckScreenDefaultWindowOpened;
			screen.WindowClosed += WnckScreenDefaultWindowClosed;
		}

		#region Window Setup
		void WnckScreenDefaultWindowOpened (object o, WindowOpenedArgs args)
		{
			SetupWindow (args.Window);
		}

		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			if (args.Window != null)
				window_to_desktop_files.Remove (args.Window);
		}
		
		void SetupWindow (Wnck.Window window)
		{
			window_to_desktop_files [window] = FindDesktopFileForWindowOrDefault (window).ToList ();
			
			window.NameChanged += delegate {
				window_to_desktop_files [window] = FindDesktopFileForWindowOrDefault (window).ToList ();
			};
		}
		#endregion
		
		public IEnumerable<Wnck.Window> WindowsForDesktopFile (string desktop_file)
		{
			if (desktop_file == null)
				throw new ArgumentNullException ("desktop file");
			
			foreach (KeyValuePair<Wnck.Window, List<string>> kvp in window_to_desktop_files) {
				if (kvp.Value.Contains (desktop_file))
					yield return kvp.Key;
			}
		}
		
		public IEnumerable<Wnck.Window> SimilarWindows (Wnck.Window window)
		{
			if (window == null)
				throw new ArgumentNullException ("window");
			
			if (!window_to_desktop_files.ContainsKey (window))
				return new[] { window };
			
			string id = window_to_desktop_files [window].DefaultIfEmpty ("").FirstOrDefault ();
			
			return window_to_desktop_files
				.Where (kvp => kvp.Value.Contains (id))
				.Select (kvp => kvp.Key);
		}
		
		public string DesktopFileForWindow (Wnck.Window window)
		{
			if (window == null)
				throw new ArgumentNullException ("window");
			
			if (WindowIsOpenOffice (window))
				SetupWindow (window);
			
			string file = window_to_desktop_files[window].FirstOrDefault ();

			if (file == null)
				file = "";
			file = file.EndsWith (".desktop") ? file : null;
			
			return file;
		}
		
		public bool WindowIsReadyForMatch (Wnck.Window window)
		{
			if (!WindowIsOpenOffice (window))
				return true;
			SetupWindow (window);
			string win = window_to_desktop_files[window].FirstOrDefault ();
			if (win == null) win = "";
			return win.EndsWith (".desktop");
		}
		
		bool WindowIsOpenOffice (Wnck.Window window)
		{
			return window.ClassGroup != null && window.ClassGroup.Name.ToLower ().StartsWith ("openoffice");
		}
		
		IEnumerable<string> FindDesktopFileForWindowOrDefault (Wnck.Window window)
		{
			int pid = window.Pid;
			if (pid <= 1) {
				if (window.ClassGroup != null && !string.IsNullOrEmpty (window.ClassGroup.ResClass)) {
					yield return window.ClassGroup.ResClass;
					yield break;
				} else {
					yield return window.Name;
					yield break;
				}
			}
			
			bool matched = false;
			int currentPid = 0;
			
			// get ppid and parents
			IEnumerable<int> pids = PIDAndParents (pid);
			// this list holds a list of the command line parts from left (0) to right (n)
			List<string> command_line = new List<string> ();
			
			// if we have a classname that matches a desktopid we have a winner
			if (window.ClassGroup != null) {
				if (WindowIsOpenOffice (window)) {
					string title = window.Name;
					if (title.Contains ("Writer"))
						command_line.Add ("ooffice-writer");
					else if (title.Contains ("Draw"))
						command_line.Add ("ooffice-draw");
					else if (title.Contains ("Impress"))
						command_line.Add ("ooffice-impress");
					else if (title.Contains ("Calc"))
						command_line.Add ("ooffice-calc");
					else if (title.Contains ("Math"))
						command_line.Add ("ooffice-math");
				} else {
					string class_name = window.ClassGroup.ResClass.Replace (".", "");
					IEnumerable<string> matches = Enumerable.Empty<string> ();
					try {
						matches = DesktopFiles
							.Where (file => Path.GetFileNameWithoutExtension (file).Equals (class_name, StringComparison.CurrentCultureIgnoreCase));
					} catch (Exception e) {
						Docky.Services.Log<WindowMatcher>.Error (e.Message);
						Docky.Services.Log<WindowMatcher>.Debug (e.StackTrace);
					}
					
					foreach (string s in matches) {
						yield return s;
						matched = true;
					}
				}
			}
			
			do {
				command_line.AddRange (CommandLineForPid (pids.ElementAt (currentPid++)).Where (cmd => !string.IsNullOrEmpty (cmd)));
				if (command_line.Count () == 0)
					continue;
				foreach (string cmd in command_line) {
					if (exec_to_desktop_files.ContainsKey (cmd)) {
						foreach (string s in exec_to_desktop_files[cmd]) {
							if (string.IsNullOrEmpty (s))
								continue;
							yield return s;
							matched = true;
						}
					}
				}
				// if we found a match, bail.
				if (matched)
					yield break;
				command_line.Clear ();
			} while (currentPid < pids.Count ());
			
			// if no match was found, just return the pid
			yield return window.Pid.ToString ();
		}
		
		IEnumerable<int> PIDAndParents (int pid)
		{
			string cmdline;

			do {
				yield return pid;
				
				try {
					string procPath = new [] { "/proc", pid.ToString (), "stat" }.Aggregate (Path.Combine);
					using (StreamReader reader = new StreamReader (procPath)) {
						cmdline = reader.ReadLine ();
						reader.Close ();
					}
				} catch { 
					yield break; 
				}
				
				if (cmdline == null)
					yield break;
				
				cmdline = cmdline.ToLower ();
				
				string [] result = cmdline.Split (Convert.ToChar (0x0)) [0].Split (' ');

				if (result.Count () < 4)
					yield break;
				
				// the ppid is index number 3
				if (!int.TryParse (result [3], out pid))
					yield break;
			} while (pid != 1);
		}
		
		string [] CommandLineForPid (int pid)
		{
			string cmdline;

			try {
				string procPath = new [] { "/proc", pid.ToString (), "cmdline" }.Aggregate (Path.Combine);
				using (StreamReader reader = new StreamReader (procPath)) {
					cmdline = reader.ReadLine ();
					reader.Close ();
				}
			} catch { return new string[0]; }
			
			if (cmdline == null)
				return new string[0];
			
			cmdline = cmdline.ToLower ();
			
			string [] result = cmdline.Split (Convert.ToChar (0x0));
			
			return result
				.Select (s => s.Split (new []{'/', '\\'}).Last ())
				.Where (s => !prefix_filters.Any (f => f.IsMatch (s)))
				.ToArray ();
		}
		
		Dictionary<string, List<string>> BuildExecStrings ()
		{
			Dictionary<string, List<string>> result = new Dictionary<string, List<string>> ();
			
			foreach (string file in DesktopFiles) {
				DesktopItem item = new DesktopItem (file);
				if (item == null || !item.HasAttribute ("Exec"))
					continue;
				
				if (item.HasAttribute ("NoDisplay") && item.GetBool ("NoDisplay"))
					continue;
				
				string exec = item.GetString ("Exec");
				string vexec = null;
				
				if (exec.StartsWith ("ooffice") && exec.Contains (' ')) {
					vexec = "ooffice" + exec.Split (' ') [1];
				} else {
					string [] parts = exec.Split (' ');
					
					vexec = parts
						.DefaultIfEmpty (null)
						.Select (part => part.Split (new [] {
						'/',
						'\\'
					}).Last ())
						.Where (part => !prefix_filters.Any (f => f.IsMatch (part)))
						.FirstOrDefault ();
				}
				
				if (vexec == null)
					continue;
				
				if (!result.ContainsKey (vexec))
					result [vexec] = new List<string> ();
				result [vexec].Add (file);
				item.Dispose ();
			}
			
			return result;
		}
		
		List<Regex> BuildPrefixFilters ()
		{
			return new List<Regex> (PrefixStrings.Select (s => new Regex ("^" + s + "$")));
		}
	}
}
