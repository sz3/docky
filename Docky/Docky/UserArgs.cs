//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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

using Mono.Options;

namespace Docky
{
	public class UserArgs
	{
		const int FORCE_BUFFER_REFRESH = 10;
		public static bool NoPollCursor { get; private set; }
		public static int MaxSize { get; private set; }
		public static bool NetbookMode { get; private set; }
		public static uint BufferTime { get; private set; }
		public static bool HelpShown { get; private set; }
		
		static OptionSet Options { get; set; }
		
		static UserArgs ()
		{
			MaxSize = int.MaxValue;
			
			Options = new OptionSet () {
				{ "p|disable-polling", "Disable cursor polling (for testing)", val => NoPollCursor = true },
				{ "m|max-size=", "Maximum window dimension (min 500)", (int max) => MaxSize = Math.Max (max, 500) },
				{ "d|debug", "Enable debug logging", debug => {
						Log.DisplayLevel = (debug == null) ? LogLevel.Warn : LogLevel.Debug;
					} },
				{ "n|netbook", "Netbook mode", netbook => NetbookMode = true },
				{ "nv|nvidia", "Nvidia mode (for Nvidia cards that lag after a while).  Equivalent to '-b 10'.",
					nv => {
						if (BufferTime == 0)
							BufferTime = FORCE_BUFFER_REFRESH;
					} },
				{ "b|buffer-time=", "Maximum time (in minutes) to keep buffers", (uint buf) => BufferTime = buf },
				{ "h|?|help", "Show this help list", help => ShowHelp () },
			};
		}

		public static void Parse (string[] args)
		{
			try {
				Options.Parse (args);
			} catch (OptionException ex) {
				Log<UserArgs>.Error ("Error parsing options: {0}", ex.Message);
				ShowHelp ();
			}
			
			// if the buffer time wasn't explicity set, and a Nvidia card is present,
			// force the buffer refresh time to 10 minutes
			if (DockServices.System.HasNvidia && BufferTime == 0)
				BufferTime = FORCE_BUFFER_REFRESH;
			
			// log the parsed user args
			Log<UserArgs>.Debug ("BufferTime = " + BufferTime);
			Log<UserArgs>.Debug ("MaxSize = " + MaxSize);
			Log<UserArgs>.Debug ("NetbookMode = " + NetbookMode);
			Log<UserArgs>.Debug ("NoPollCursor = " + NoPollCursor);
		}
		
		public static void ShowHelp ()
		{
			Console.WriteLine ("usage: docky [options]");
			Console.WriteLine ();
			Options.WriteOptionDescriptions (Console.Out);
			HelpShown = true;
		}
	}
}
