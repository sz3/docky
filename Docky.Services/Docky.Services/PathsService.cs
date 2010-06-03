//  
//  Copyright (C) 2010 Chris Szikszoy, Robert Dyer
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
		public File SystemDataFolder { get; protected set; }
		
		public File DockManagerUserDataFolder { get; protected set; }
		
		public File UserDataFolder { get; protected set; }
		
		public File UserCacheFolder { get; protected set; }
		
		public File AutoStartFile { get; protected set; }
		
		public PathsService ()
		{
			// get environment-based settings
			File env_home         = FileFactory.NewForPath (Environment.GetEnvironmentVariable ("HOME"));
			File env_data_home    = FileFactory.NewForPath (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData));
			File env_data         = FileFactory.NewForPath (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData));
			File env_data_install = FileFactory.NewForPath (AssemblyInfo.DataDirectory);
			
			
			// get XDG Base Directory settings
			string xdg_data_home  = Environment.GetEnvironmentVariable ("XDG_DATA_HOME");
			string xdg_data_dirs  = Environment.GetEnvironmentVariable ("XDG_DATA_DIRS");
			string xdg_cache_home = Environment.GetEnvironmentVariable ("XDG_CACHE_HOME");
			
			
			// determine directories based on XDG with fallbacks
			File cache_folder;
			if (!string.IsNullOrEmpty (xdg_cache_home))
				cache_folder = FileFactory.NewForPath (xdg_cache_home);
			else
				cache_folder = env_home.GetChild (".cache");
			
			File data_folder;
			if (!string.IsNullOrEmpty (xdg_data_home))
				data_folder = FileFactory.NewForPath (xdg_data_home);
			else
				data_folder = env_data_home;
			
			
			// set the XDG Base Directory specified directories to use
			UserCacheFolder           = cache_folder.GetChild ("docky");
			DockManagerUserDataFolder = data_folder.GetChild ("dockmanager");
			UserDataFolder            = data_folder.GetChild ("docky");
			
			
			// set the non-XDG Base Directory specified directories to use
			SystemDataFolder = env_data_install.GetChild ("docky");
			AutoStartFile    = env_data.GetChild ("autostart").GetChild ("docky.desktop");
			
			
			// ensure all writable directories exist
			EnsureDirectoryExists (UserCacheFolder);
			EnsureDirectoryExists (DockManagerUserDataFolder);
			EnsureDirectoryExists (UserDataFolder);
		}
		
		static void EnsureDirectoryExists (File dir)
		{
			if (!dir.Exists)
				try {
					dir.MakeDirectoryWithParents (null);
				} catch {
					Log<PathsService>.Fatal ("Could not access the directory '" + dir.Path + "' or create it.  Docky will not work properly unless this folder is writable.");
				}
		}
	}
}

