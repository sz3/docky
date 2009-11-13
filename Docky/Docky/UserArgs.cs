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

using Docky.Services;

using Mono.GetOptions;

namespace Docky
{


	public class UserArgs : Options
	{
		public LogLevel Logging { get; protected set; }
		
		[Option ("Disable cursor polling (for testing)", 'p', "disable-polling")]
		public bool NoPollCursor;
		
		[Option ("Maximum window dimension (min 500)", 'm', "max-size")]
		public int MaxSize;
		
		[Option ("Enable debug level logging", 'd', "debug")]
		public bool Debug;
		
		[Option ("Netbook mode", 'n', "netbook")]
		public bool NetbookMode;

		public UserArgs (string[] args)
		{
			ProcessArgs (args);
			ParsingMode = OptionsParsingMode.GNU_DoubleDash;
			
			// defaults
			if (MaxSize == 0)
				MaxSize = int.MaxValue;
			MaxSize = Math.Max (MaxSize, 500);
			Logging = LogLevel.Warn;
			// if the debug option was passed, set it to debug
			// otherwise leave it to the default, which is warn
			if (Debug)
				Log.DisplayLevel = LogLevel.Debug; 
		}
	}
}
