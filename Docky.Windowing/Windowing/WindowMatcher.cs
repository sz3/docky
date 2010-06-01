//  
//  Copyright (C) 2009-2010 Jason Smith, Robert Dyer, Chris Szikszoy, 
//                          Rico Tzschichholz
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
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Wnck;

using Docky.Services;

namespace Docky.Windowing
{
	public class WindowMatcher
	{
		public static event EventHandler<DesktopFileChangedEventArgs> DesktopFileChanged;
		
		static WindowMatcher ()
		{
			Default = new WindowMatcher ();
		}
		
		public static WindowMatcher Default { get; protected set; }
		
		List<DesktopItem> custom_desktop_items;
		List<DesktopItem> desktop_items;
		IEnumerable<DesktopItem> DesktopItems { 
			get {
				return custom_desktop_items.Union (desktop_items).AsEnumerable ();
			}
		}
		
		static string[] LocaleEnvVariables = new[] { "LC_ALL", "LC_MESSAGES", "LANG", "LANGUAGE" };
		static string locale;
		public static string Locale 
		{
			get {
				if (!string.IsNullOrEmpty (locale))
					return locale;
			
				string loc;
				foreach (string env in LocaleEnvVariables) {
					loc = Environment.GetEnvironmentVariable (env);
					if (!string.IsNullOrEmpty (loc) && loc.Length >= 2) {
						locale = loc;
						return locale;
					}
				}
				
				locale = "";
				return locale;
			}
		}

		IEnumerable<Wnck.Window> UnmatchedWindows {
			get {
				IEnumerable<Wnck.Window> matched = window_to_desktop_items.Keys.Cast<Wnck.Window> ();
				return Wnck.Screen.Default.Windows
					.Where (w => !w.IsSkipTasklist && !matched.Contains (w));
			}
		}
		
		object update_lock;
		
		Dictionary<Wnck.Window, List<DesktopItem>> window_to_desktop_items;
		Dictionary<string, List<DesktopItem>> exec_to_desktop_items;
		Dictionary<string, DesktopItem> class_to_desktop_items;
		Dictionary<string, string> remap_items;
		readonly List<Regex> prefix_filters;
		readonly List<Regex> suffix_filters;
		
		void DesktopItemsChanged ()
		{
			exec_to_desktop_items = BuildExecStrings ();
			class_to_desktop_items = BuildClassStrings ();
		}
		
		private WindowMatcher ()
		{
			Log<WindowMatcher>.Debug ("Initialize WindowMatcher");

			Wnck.Screen screen = Wnck.Screen.Default;

			update_lock = new object ();
			prefix_filters = BuildPrefixFilters ();
			suffix_filters = BuildSuffixFilters ();
			
			Log<WindowMatcher>.Debug ("Loading Remaps..");
			remap_items = new Dictionary<string, string> ();
			LoadRemaps (DockServices.Paths.SystemDataFolder.GetChild ("remaps.ini"));
			LoadRemaps (DockServices.Paths.UserDataFolder.GetChild ("remaps.ini"));
			
			// Load DesktopFilesCache from docky.desktop.[LANG].cache
			desktop_items = LoadDesktopItemsCache (DockyDesktopFileCacheFile);
			custom_desktop_items = new List<DesktopItem> ();
			
			if (desktop_items == null || desktop_items.Count () == 0) {
				Log<WindowMatcher>.Info ("Loading *.desktop files and regenerating cache. This may take some while...");
				UpdateDesktopItemsList ();
				ProcessAndMergeAllSystemCacheFiles (desktop_items);
				SaveDesktopItemsCache();
			}
			DesktopItemsChanged ();

			// Update desktop_items and save cache after 2 minutes just to be sure we are up to date
			GLib.Timeout.Add (2 * 60 * 1000, delegate {
				lock (update_lock) {
					UpdateDesktopItemsList ();
					ProcessAndMergeAllSystemCacheFiles (desktop_items);
					DesktopItemsChanged ();
					SaveDesktopItemsCache();
				}
				return false;
			});
			
			// Initialize window matching with currently available windows
			window_to_desktop_items = new Dictionary<Wnck.Window, List<DesktopItem>> ();
			foreach (Wnck.Window w in screen.Windows)
				SetupWindow (w);

			// Set up monitors for cache files and desktop directories
			foreach (GLib.File dir in DesktopFileDirectories.Select (d => GLib.FileFactory.NewForPath (d)))
				MonitorDesktopFileDirs (dir);
			MonitorDesktopFileSystemCacheFiles ();
			
			screen.WindowOpened += WnckScreenDefaultWindowOpened;
			screen.WindowClosed += WnckScreenDefaultWindowClosed;
		}

		#region Handle DesktopItems
		static IEnumerable<string> DesktopFileSystemCacheFiles
		{
			get {
				return DesktopFileDirectories
					.Select (d => Path.Combine (d, string.Format ("desktop.{0}.cache", Locale)))
					.Where (f => File.Exists (f));
			}
		}
		
		static string DockyDesktopFileCacheFile
		{
			get {
				if (!string.IsNullOrEmpty (Locale))
					return DockServices.Paths.UserCacheFolder.GetChild (string.Format ("docky.desktop.{0}.cache", Locale)).Path;
				return DockServices.Paths.UserCacheFolder.GetChild ("docky.desktop.cache").Path;
			}
		}
			
		static IEnumerable<string> DesktopFileDirectories
		{
			get {
				return new [] {
					// These are XDG variables...
					"XDG_DATA_HOME",
					"XDG_DATA_DIRS",
					// Crossover apps
					"CX_APPS",
				}.SelectMany (v => ExpandPathVar (v))
				 .Where (d => Directory.Exists (d));
			}
		}
		
		static IEnumerable<string> ExpandPathVar (string xdgVar)
		{
			string envPath = Environment.GetEnvironmentVariable (xdgVar);
			
			if (string.IsNullOrEmpty (envPath)) {
				switch (xdgVar) {
				case "XDG_DATA_HOME":
					yield return Path.Combine (
						Environment.GetFolderPath (Environment.SpecialFolder.Personal),
						new [] {".local", "share", "applications"}.Aggregate ((w, s) => Path.Combine (w, s))
					);
					break;
				case "XDG_DATA_DIRS":
					yield return new [] {"/usr", "local", "share", "applications"}.Aggregate ((w, s) => Path.Combine (w,s));
					yield return new [] {"/usr", "share", "applications"}.Aggregate ((w, s) => Path.Combine (w, s));
					break;
				case "CX_APPS":
					yield return Path.Combine (
						Environment.GetFolderPath (Environment.SpecialFolder.Personal),
						".cxoffice"
					);
					break;
				}
			} else {
				foreach (string dir in envPath.Split (':'))
					yield return Path.Combine (dir, "applications");
			}
		}

		void UpdateDesktopItemsList ()
		{
			if (desktop_items == null)
				desktop_items = new List<DesktopItem> ();
			
			List<GLib.File> known_desktop_files = desktop_items.Select (item => item.File).ToList ();
			List<DesktopItem> new_items = new List<DesktopItem> ();

			// Get desktop items for new "valid" desktop files
			new_items = DesktopFileDirectories
				.SelectMany (dir => GLib.FileFactory.NewForPath (dir).SubDirs ())
				.Union (DesktopFileDirectories.Select (f => GLib.FileFactory.NewForPath (f)))
				.SelectMany (file => file.GetFiles (".desktop"))
				.Where (file => !known_desktop_files.Exists (known_file => (known_file.Path == file.Path)))
				.Select (file => new DesktopItem (file))
				.Where (item => item.Values.Any ())
				.ToList ();
			
			desktop_items.AddRange (new_items);

			if (new_items.Count () > 0)
				Log<WindowMatcher>.Debug ("{0} new applications found", new_items.Count ());

			// Check file existence and remove unlinked items
			int removed = desktop_items.RemoveAll (item => !item.File.Exists);
			if (removed > 0)
				Log<WindowMatcher>.Debug ("{0} applications removed", removed);
			
			known_desktop_files.Clear ();
			new_items.Clear ();
		}		
		
		void ProcessAndMergeAllSystemCacheFiles (List<DesktopItem> items)
		{
			foreach (string cache_file in DesktopFileSystemCacheFiles)
				ProcessAndMergeSystemCacheFile (cache_file, items);
		}

		void ProcessAndMergeSystemCacheFile (string cache_file, List<DesktopItem> items)
		{
			if (!GLib.FileFactory.NewForPath (cache_file).Exists)
			    return;
			
			Log<WindowMatcher>.Debug ("Processing {0}", cache_file);
			
			try {
				using (StreamReader reader = new StreamReader (cache_file)) {
					DesktopItem desktop_item = null;
					string line;
					
					while ((line = reader.ReadLine ()) != null) {
						if (line.Trim ().Length <= 0)
							continue;
						
						if (line.ElementAt (0) == '[') {
							Match match = DesktopItem.sectionRegex.Match (line);
							if (match.Success) {
								string section = match.Groups["Section"].Value;
								if (section != null) {
									GLib.File file = GLib.FileFactory
										.NewForPath (Path.Combine (Path.GetDirectoryName (cache_file), 
										                           string.Format ("{0}.desktop", section)));
									desktop_item = items.First (item => item.File.Path == file.Path);
									if (desktop_item == null && file.Exists) {
										desktop_item = new DesktopItem (file);
										items.Add (desktop_item);
										Log<WindowMatcher>.Debug ("New application found: {0}", desktop_item.Path);
									}
									continue;
								}
							}
						} else if (desktop_item != null) {
							Match match = DesktopItem.keyValueRegex.Match (line);
							if (match.Success) {
								string key = match.Groups["Key"].Value;
								string val = match.Groups["Value"].Value;
								if (!string.IsNullOrEmpty (key) && !string.IsNullOrEmpty (val))
									desktop_item.SetString (key, val);
								continue;
							}
						}
					}
					reader.Close ();
				}
			} catch (Exception e) {
				Log<WindowMatcher>.Error (e.Message);
				Log<WindowMatcher>.Error (e.StackTrace);
			}
		}

		void LoadRemaps (GLib.File file)
		{
			if (!file.Exists)
				return;
			
			Regex keyValueRegex = new Regex (
				@"(^(\s)*(?<Key>([^\=^\n]+))[\s^\n]*\=(\s)*(?<Value>([^\n]+(\n){0,1})))",
				RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | 
				RegexOptions.CultureInvariant
			);
			
			try {
				using (StreamReader reader = new StreamReader (file.Path)) {
					string line;
					
					while ((line = reader.ReadLine ()) != null) {
						line = line.Trim ();
						if (line.Length <= 0 || line.Substring (0, 1) == "#")
							continue;
						
						Match match = keyValueRegex.Match (line);
						if (match.Success) {
							string key = match.Groups["Key"].Value;
							string val = match.Groups["Value"].Value;
							if (!string.IsNullOrEmpty (key)) {
								remap_items[key] = val;
								Log<WindowMatcher>.Debug ("Remapping '" + key + "' to '" + val + "'");
							}
						}
					}
					reader.Close ();
				}
			} catch (Exception e) {
				Log<WindowMatcher>.Error (e.Message);
				Log<WindowMatcher>.Error (e.StackTrace);
			}
		}
		
		List<DesktopItem> LoadDesktopItemsCache (string filename)
		{
			if (!GLib.FileFactory.NewForPath (filename).Exists)
				return new List<DesktopItem> ();
				
			Log<WindowMatcher>.Debug ("Loading {0}", DockyDesktopFileCacheFile);
			
			List<DesktopItem> items = new List<DesktopItem> ();
			
			try {
				using (StreamReader reader = new StreamReader (filename)) {
					DesktopItem desktop_item = null;
					string line;
					
					while ((line = reader.ReadLine ()) != null) {
						if (line.Trim ().Length <= 0)
							continue;
						
						if (line.ElementAt (0) == '[') {
							Match match = DesktopItem.sectionRegex.Match (line);
							if (match.Success) {
								string section = match.Groups["Section"].Value;
								if (section != null) {
									GLib.File file = GLib.FileFactory.NewForPath (section);
									desktop_item = new DesktopItem (file, false);
									items.Add (desktop_item);
								}
								continue;
							}
						} else if (desktop_item != null) {
							Match match = DesktopItem.keyValueRegex.Match (line);
							if (match.Success) {
								string key = match.Groups["Key"].Value;
								string val = match.Groups["Value"].Value;
								if (!string.IsNullOrEmpty (key) && !string.IsNullOrEmpty (val))
									desktop_item.SetString (key, val);
								continue;
							}
						}
					}
					reader.Close ();
				}
			} catch (Exception e) {
				Log<WindowMatcher>.Error (e.Message);
				Log<WindowMatcher>.Error (e.StackTrace);
				return null;
			}

			return items;
		}

		void SaveDesktopItemsCache ()
		{
			Log<WindowMatcher>.Debug ("Saving {0}", DockyDesktopFileCacheFile);

			try {
				using (StreamWriter writer = new StreamWriter (DockyDesktopFileCacheFile, false)) {
					foreach (DesktopItem item in desktop_items) {
						writer.WriteLine ("[{0}]", item.Path);
						IDictionaryEnumerator enumerator = item.Values.GetEnumerator ();
						enumerator.Reset();
						while (enumerator.MoveNext())
							writer.WriteLine ("{0}={1}", enumerator.Key, enumerator.Value);
						writer.WriteLine ("");
					}
					writer.Close ();
				}
			} catch (Exception e) {
				Log<WindowMatcher>.Error (e.Message);
				Log<WindowMatcher>.Error (e.StackTrace);
			}
		}

		void MonitorDesktopFileSystemCacheFiles ()
		{
			foreach (string filename in DesktopFileSystemCacheFiles) {
				GLib.File file = GLib.FileFactory.NewForPath (filename);
				GLib.FileMonitor mon = file.Monitor (GLib.FileMonitorFlags.None, null);
				mon.RateLimit = 2500;
				mon.Changed += delegate {
					DockServices.System.RunOnThread (() => {
						lock (update_lock) {
							ProcessAndMergeSystemCacheFile (file.Path, desktop_items);
							DesktopItemsChanged ();
						}   
					});
				};
			}
		}
		
		void MonitorDesktopFileDirs (GLib.File dir)
		{
			// build a list of all the subdirectories
			List<GLib.File> dirs = new List<GLib.File> () {dir};
			try {
				dirs = dirs.Union (dir.SubDirs ()).ToList ();	
			} catch {}
			
			foreach (GLib.File d in dirs) {
				GLib.FileMonitor mon = d.Monitor (GLib.FileMonitorFlags.None, null);
				mon.RateLimit = 2500;
				mon.Changed += delegate(object o, GLib.ChangedArgs args) {
					// bug in GIO#, calling args.File or args.OtherFile crashes hard
					GLib.File file = GLib.FileAdapter.GetObject ((GLib.Object) args.Args[0]);
					GLib.File otherFile = GLib.FileAdapter.GetObject ((GLib.Object) args.Args[1]);

					// according to GLib documentation, the change signal runs on the same
					// thread that the monitor was created on.  Without running this part on a thread
					// docky freezes up for about 500-800 ms while the .desktop files are parsed.
					DockServices.System.RunOnThread (() => {
						// if a new directory was created, make sure we watch that dir as well
						if (file.QueryFileType (GLib.FileQueryInfoFlags.NofollowSymlinks, null) == GLib.FileType.Directory)
							MonitorDesktopFileDirs (file);
						// we only care about .desktop files
						if (!file.Path.EndsWith (".desktop"))
							return;

						lock (update_lock) {
							UpdateDesktopItemsList ();
							DesktopItemsChanged ();
							SaveDesktopItemsCache();
						}
						
						// Make sure to trigger event on main thread
						DockServices.System.RunOnMainThread (() => {
							if (DesktopFileChanged != null) {
								DesktopFileChanged (this, new DesktopFileChangedEventArgs (args.EventType, file, otherFile));
							}
						});
					});
				};
			}
		}

		public void RegisterDesktopItem (DesktopItem item)
		{
			if (DesktopItems.Contains (item))
				return;
			
			custom_desktop_items.Add (item);
			
			DesktopItemsChanged ();
		}
		#endregion

		#region Window Setup
		void WnckScreenDefaultWindowOpened (object o, WindowOpenedArgs args)
		{
			SetupWindow (args.Window);
		}

		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			if (args.Window != null)
				window_to_desktop_items.Remove (args.Window);
		}
		
		bool SetupWindow (Wnck.Window window)
		{
			IEnumerable<DesktopItem> items = DesktopItemsForWindow (window);
			if (items.Any ()) {
				window_to_desktop_items [window] = items.ToList ();
				return true;
			} else
				return false;
		}
		#endregion

		#region Window Matching
		static IEnumerable<string> PrefixStrings {
			get {
				yield return "gksu(do)?";
				yield return "sudo";
				yield return "java";
				yield return "mono";
				yield return "ruby";
				yield return "padsp";
				yield return "perl";
				yield return "aoss";
				yield return "python(\\d+.\\d+)?";
				yield return "wish(\\d+\\.\\d+)?";
				yield return "(ba)?sh";
				yield return "-.*";
				yield return "*.\\.desktop";
			}
		}
		
		List<Regex> BuildPrefixFilters ()
		{
			return new List<Regex> (PrefixStrings.Select (s => new Regex ("^" + s + "$")));
		}
		
		static IEnumerable<string> SuffixStrings {
			get {
				// some wine apps are launched via a shell script that sets the proc name to "app.exe"
				yield return ".exe";
				// some apps have a script 'foo' which does 'exec foo-bin'
				yield return "-bin";
				// some python apps have a script 'foo' for 'python foo.py'
				yield return ".py";
				// some apps append versions, such as '-1' or '-3.0'
				yield return "(-)?\\d+(\\.\\d+)?";
			}
		}

		List<Regex> BuildSuffixFilters ()
		{
			return new List<Regex> (SuffixStrings.Select (s => new Regex (s + "$")));
		}

		public bool WindowIsReadyForMatch (Wnck.Window window)
		{
			if (!WindowIsOpenOffice (window))
				return true;

			return SetupWindow (window);
		}
		
		public bool WindowIsOpenOffice (Wnck.Window window)
		{
			return window.ClassGroup != null && window.ClassGroup.Name.ToLower ().StartsWith ("openoffice");
		}
		
		public IEnumerable<Wnck.Window> WindowsForDesktopItem (DesktopItem item)
		{
			if (item == null)
				throw new ArgumentNullException ("DesktopItem item");
			
			foreach (KeyValuePair<Wnck.Window, List<DesktopItem>> kvp in window_to_desktop_items)
				if (kvp.Value.Any (df => df == item))
					yield return kvp.Key;
		}
		
		public IEnumerable<Wnck.Window> SimilarWindows (Wnck.Window window)
		{
			if (window == null)
				throw new ArgumentNullException ("Wnck.Window window");
			
			//TODO perhaps make it a bit smarter
			if (!window_to_desktop_items.ContainsKey (window))
				foreach (Wnck.Window win in UnmatchedWindows) {
					if (win == window)
						continue;
					
					if (win.Pid == window.Pid)
						yield return win;
					else if (window.Pid <= 1) {
						if (window.ClassGroup != null
								&& !string.IsNullOrEmpty (window.ClassGroup.ResClass)
								&& win.ClassGroup.ResClass.Equals (window.ClassGroup.ResClass))
							yield return win;
						else if (!string.IsNullOrEmpty (win.Name) && win.Name.Equals (window.Name)) 
							yield return win;
					}
				}
			
			yield return window;
		}

		public DesktopItem DesktopItemForDesktopFile (string file)
		{
			try {
				return DesktopItems
					.Where (df => df.Path.Equals (file, StringComparison.CurrentCultureIgnoreCase))
					.DefaultIfEmpty (null)
					.FirstOrDefault ();
			} catch (Exception e) {
				Docky.Services.Log<WindowMatcher>.Error (e.Message);
				Docky.Services.Log<WindowMatcher>.Debug (e.StackTrace);
			}
			
			return null;
		}
		
		public DesktopItem DesktopItemForWindow (Wnck.Window window)
		{
			if (window == null)
				throw new ArgumentNullException ("window");
			
			List<DesktopItem> item;
			if (window_to_desktop_items.TryGetValue (window, out item))
				return item.FirstOrDefault ();
			
			return null;
		}
		
		IEnumerable<DesktopItem> DesktopItemsForDesktopID (string id)
		{
			IEnumerable<DesktopItem> matches = Enumerable.Empty<DesktopItem> ();
			try {
				matches = DesktopItems
					.Where (df => df.DesktopID.Equals (id, StringComparison.CurrentCultureIgnoreCase));
			} catch (Exception e) {
				Docky.Services.Log<WindowMatcher>.Error (e.Message);
				Docky.Services.Log<WindowMatcher>.Debug (e.StackTrace);
			}
			
			return matches;
		}
		
		IEnumerable<DesktopItem> DesktopItemsForWindow (Wnck.Window window)
		{
			// use the StartupWMClass as the definitive match
			if (window.ClassGroup != null
					&& !string.IsNullOrEmpty (window.ClassGroup.ResClass)
					&& window.ClassGroup.ResClass != "Wine"
					&& class_to_desktop_items.ContainsKey (window.ClassGroup.ResClass)) {
				yield return class_to_desktop_items [window.ClassGroup.ResClass];
				yield break;
			}
			
			int pid = window.Pid;
			if (pid <= 1) {
				if (window.ClassGroup != null && !string.IsNullOrEmpty (window.ClassGroup.ResClass)) {
					IEnumerable<DesktopItem> matches = DesktopItemsForDesktopID (window.ClassGroup.ResClass);
					if (matches.Any ())
						foreach (DesktopItem s in matches)
							yield return s;
				}
				yield break;
			}
			
			bool matched = false;
			int currentPid = 0;
			
			// get ppid and parents
			IEnumerable<int> pids = PidAndParents (pid);
			// this list holds a list of the command line parts from left (0) to right (n)
			List<string> command_line = new List<string> ();
			
			// if we have a classname that matches a desktopid we have a winner
			if (window.ClassGroup != null) {
				if (WindowIsOpenOffice (window)) {
					string title = window.Name.Trim ();
					if (title.EndsWith ("Writer"))
						command_line.Add ("ooffice-writer");
					else if (title.EndsWith ("Draw"))
						command_line.Add ("ooffice-draw");
					else if (title.EndsWith ("Impress"))
						command_line.Add ("ooffice-impress");
					else if (title.EndsWith ("Calc"))
						command_line.Add ("ooffice-calc");
					else if (title.EndsWith ("Math"))
						command_line.Add ("ooffice-math");
				} else if (window.ClassGroup.ResClass == "Wine") {
					// we can match Wine apps normally so don't do anything here
				} else {
					string class_name = window.ClassGroup.ResClass.Replace (".", "");
					IEnumerable<DesktopItem> matches = DesktopItemsForDesktopID (class_name);
					
					foreach (DesktopItem s in matches) {
						yield return s;
						matched = true;
					}
				}
			}
	
			lock (update_lock) {
				do {
					// do a match on the process name
					string name = NameForPid (pids.ElementAt (currentPid));
					if (exec_to_desktop_items.ContainsKey (name)) {
						foreach (DesktopItem s in exec_to_desktop_items[name]) {
							//if (string.IsNullOrEmpty (s))
							//	continue;
							yield return s;
							matched = true;
						}
					}
					
					// otherwise do a match on the commandline
					command_line.AddRange (CommandLineForPid (pids.ElementAt (currentPid++))
						.Select (cmd => cmd.Replace (@"\", @"\\")));
					
					if (command_line.Count () == 0)
						continue;
					
					foreach (string cmd in command_line) {
						if (exec_to_desktop_items.ContainsKey (cmd)) {
							foreach (DesktopItem s in exec_to_desktop_items[cmd]) {
								//if (string.IsNullOrEmpty (s))
								//	continue;
								yield return s;
								matched = true;
							}
						}
					}
					
					// if we found a match, bail.
					if (matched)
						yield break;
				} while (currentPid < pids.Count ());
			}
			command_line.Clear ();
			yield break;
		}
		
		IEnumerable<int> PidAndParents (int pid)
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
				
				string [] result = cmdline.Split (Convert.ToChar (0x0)) [0].Split (' ');

				if (result.Count () < 4)
					yield break;
				
				// the ppid is index number 3
				if (!int.TryParse (result [3], out pid))
					yield break;
			} while (pid > 1);
		}
		
		IEnumerable<string> CommandLineForPid (int pid)
		{
			string cmdline;

			try {
				string procPath = new [] { "/proc", pid.ToString (), "cmdline" }.Aggregate (Path.Combine);
				using (StreamReader reader = new StreamReader (procPath)) {
					cmdline = reader.ReadLine ();
					reader.Close ();
				}
			} catch { yield break; }
			
			if (cmdline == null)
				yield break;
			
			cmdline = cmdline.Trim ();
						
			string [] result = cmdline.Split (Convert.ToChar (0x0));
			
			// these are sanitized results
			foreach (string sanitizedCmd in result
				.Select (s => s.Split (new []{'/', '\\'}).Last ())
			    .Distinct ()
				.Where (s => !string.IsNullOrEmpty (s) && !prefix_filters.Any (f => f.IsMatch (s)))) {
				
				yield return sanitizedCmd;
				
				if (remap_items.ContainsKey (sanitizedCmd))
					yield return remap_items [sanitizedCmd];
				
				// if it ends with a special suffix, strip the suffix and return an additional result
				foreach (Regex f in suffix_filters)
					if (f.IsMatch (sanitizedCmd))
						yield return f.Replace (sanitizedCmd, "");
			}
			
			// return the entire cmdline last as a last ditch effort to find a match
			yield return cmdline;
		}
		
		string NameForPid (int pid)
		{
			string name;

			try {
				string procPath = new [] { "/proc", pid.ToString (), "status" }.Aggregate (Path.Combine);
				using (StreamReader reader = new StreamReader (procPath)) {
					name = reader.ReadLine ();
					reader.Close ();
				}
			} catch { return ""; }
			
			if (string.IsNullOrEmpty (name) || !name.StartsWith ("Name:"))
				return "";
			
			return name.Substring (6);
		}
		
		Dictionary<string, DesktopItem> BuildClassStrings ()
		{
			Dictionary<string, DesktopItem> result = new Dictionary<string, DesktopItem> ();
			
			foreach (DesktopItem item in DesktopItems) {
				if (item == null || !item.HasAttribute ("StartupWMClass"))
					continue;
				
				if (item.HasAttribute ("NoDisplay") && item.GetBool ("NoDisplay"))
					continue;
				
				if (item.HasAttribute ("X-Docky-NoMatch") && item.GetBool ("X-Docky-NoMatch"))
					continue;
				
				string cls = item.GetString ("StartupWMClass").Trim ();
				result [cls] = item;
			}
			
			return result;
		}
				
		Dictionary<string, List<DesktopItem>> BuildExecStrings ()
		{
			Dictionary<string, List<DesktopItem>> result = new Dictionary<string, List<DesktopItem>> ();
			
			foreach (DesktopItem item in DesktopItems) {
				if (item == null || !item.HasAttribute ("Exec"))
					continue;
				
				if (item.HasAttribute ("NoDisplay") && item.GetBool ("NoDisplay"))
					continue;
				
				if (item.HasAttribute ("X-Docky-NoMatch") && item.GetBool ("X-Docky-NoMatch"))
					continue;
				
				string exec = item.GetString ("Exec").Trim ();
				string vexec = null;
				
				// for openoffice
				if (exec.Contains (' ') &&
					(exec.StartsWith ("ooffice") || exec.StartsWith ("openoffice") || exec.StartsWith ("soffice"))) {
					vexec = "ooffice" + exec.Split (' ') [1];
				
				// for wine apps
				} else if ((exec.StartsWith ("env WINEPREFIX=") && exec.Contains (" wine ")) ||
						exec.StartsWith ("wine ")) {
					int startIndex = exec.IndexOf ("wine ") + 5; // length of 'wine '
					// CommandLineForPid already splits based on \\ and takes the last entry, so do the same here
					vexec = exec.Substring (startIndex).Split (new [] {@"\\"}, StringSplitOptions.RemoveEmptyEntries).Last ();
					// remove the trailing " and anything after it
					if (vexec.Contains ("\""))
						vexec = vexec.Substring (0, vexec.IndexOf ("\""));

				// for crossover apps
				} else if (exec.Contains (".cxoffice") || (item.HasAttribute ("X-Created-By") && item.GetString ("X-Created-By").Contains ("cxoffice"))) {
					// The exec is actually another file that uses exec to launch the actual app.
					exec = exec.Replace ("\"", "");
					
					GLib.File launcher = GLib.FileFactory.NewForPath (exec);
					if (!launcher.Exists) {
						Log<WindowMatcher>.Warn ("Crossover launcher decoded as: {0}, but does not exist.", launcher.Path);
						continue;
					}
					
					string execLine = "";
					using (GLib.DataInputStream stream = new GLib.DataInputStream (launcher.Read (null))) {
						ulong len;
						string line;
						try {
							while ((line = stream.ReadLine (out len, null)) != null) {
								if (line.StartsWith ("exec")) {
									execLine = line;
									break;
								}
							}
						} catch (Exception e) {
							Log<WindowMatcher>.Error (e.Message);
							Log<WindowMatcher>.Error (e.StackTrace);
							continue;					
						}
					}
	
					// if no exec line was found, bail
					if (string.IsNullOrEmpty (execLine))
						continue;
					
					// get the relevant part from the execLine
					string [] parts = execLine.Split (new [] {'\"'});
					// find the part that contains C:/path/to/app.lnk
					if (parts.Any (part => part.StartsWith ("C:"))) {
						vexec = parts.First (part => part.StartsWith ("C:"));
						// and take only app.lnk (this is what is exposed to ps -ef)
						vexec = vexec.Split (new [] {'/'}).Last ();
					} else {
						continue;
					}
					
				// other apps
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
					
					// for AIR apps
					if (vexec != null && vexec.Contains ('\'')) {
						string strippedExec = vexec.Replace ("'", "");
						if (!result.ContainsKey (strippedExec))
							result [strippedExec] = new List<DesktopItem> ();
						result [strippedExec].Add (item);
					}
				}
				
				if (vexec == null)
					continue;
				
				if (!result.ContainsKey (vexec))
					result [vexec] = new List<DesktopItem> ();
				
				result [vexec].Add (item);
				foreach (Regex f in suffix_filters)
					if (f.IsMatch (vexec)) {
						string vexecStripped = f.Replace (vexec, "");
						if (!result.ContainsKey (vexecStripped))
							result [vexecStripped] = new List<DesktopItem> ();
						result [vexecStripped].Add (item);
					}
			}
			
			return result;
		}
		#endregion
	}
}
