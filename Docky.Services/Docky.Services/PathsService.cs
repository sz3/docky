//  
//  Copyright (C) 2010 Chris Szikszoy
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

using GLib;

namespace Docky.Services
{
	public class PathsService
	{
		static readonly File home_folder = FileFactory.NewForPath (Environment.GetEnvironmentVariable ("HOME"));
		
		public File SystemDataFolder {
			get { return FileFactory.NewForPath (AssemblyInfo.DataDirectory).GetChild ("docky"); }
		}

		public File DockManagerUserDataFolder {
			get { return FileFactory.NewForPath (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData)).GetChild ("dockmanager"); }
		}

		public File UserDataFolder {
			get { return FileFactory.NewForPath (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData)).GetChild ("docky"); }
		}
		
		File user_cache_folder;
		public File UserCacheFolder {
			get { 
				if (user_cache_folder != null)  
					return user_cache_folder;
				
				string xdg_cache_home = Environment.GetEnvironmentVariable ("XDG_CACHE_HOME");
				if (!string.IsNullOrEmpty (xdg_cache_home))
					user_cache_folder = FileFactory.NewForPath (xdg_cache_home).GetChild ("docky");
				else
					user_cache_folder = home_folder.GetChild (".cache").GetChild ("docky");
				
				if (!user_cache_folder.Exists)
					try {
						user_cache_folder.MakeDirectoryWithParents (null);
					} catch {
						Log<PathsService>.Fatal ("Could not access the cache directory '" + user_cache_folder.Path + "' or create it.  Docky will not work properly unless this folder is writable.");
					}
				
				return user_cache_folder;
			}
		}

		public File AutoStartFile {
			get { return FileFactory.NewForPath (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData)).GetChild ("autostart").GetChild ("docky.desktop"); }
		}
	}
}

