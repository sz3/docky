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
using System.ComponentModel;
using System.Linq;
using System.Text;

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

		public UserArgs ()
		{
			base.
			ParsingMode = OptionsParsingMode.GNU_DoubleDash;
			// defaults;
			Logging = LogLevel.Warn;
			MaxSize = int.MaxValue;
		}
		
		/*
		internal UserArgs (string[] args)
		{
			if (args.Contains ("--help")) {
				Console.WriteLine ("Docky - The fastest dock in the west");
				Console.WriteLine ("Usage");
				Console.WriteLine ("  docky [OPTION...]");
				Console.WriteLine ("");
				Console.WriteLine ("Arguments:");
				Console.WriteLine ("  --info                 Enable info level logging");
				Console.WriteLine ("  --debug                Enable debug level logging");
				Console.WriteLine ("  --disable-polling      Disable cursor polling (for testing)");
				Console.WriteLine ("  --max-size=SIZE        Sets the maximum window dimension (min 500)");
				Environment.Exit (0);
			}
			
			// defaults
			Logging = LogLevel.Warn;
			PoleCursor = true;
			MaxSize = int.MaxValue;
			
			args = args.SelectMany (s => s.Split ('=')).ToArray ();
			// parse the command line
			for (int i = 0; i < args.Length; i++) {
				switch (args[i]) {
				case "--info":
					Logging = LogLevel.Info;
					break;
				case "--debug":
					Logging = LogLevel.Debug;
					break;
				case "--disable-polling":
					PoleCursor = false;
					break;
				case "--max-size":
					if (i == args.Length - 1)
						break;
					int size;
					try {
						size = Convert.ToInt32 (args[i + 1]);
						i++;
					} catch (FormatException e) {
						break;
					} catch (OverflowException e) {
						break;
					}
					size = Math.Max (size, 500);
					MaxSize = size;
					break;
				}
			}
		}*/
	}
}
