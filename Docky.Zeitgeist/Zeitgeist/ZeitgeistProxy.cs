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
using System.Text;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace Zeitgeist
{


	public class ZeitgeistProxy
	{
		static DateTime UnixEpoch = new DateTime (1970, 1, 1, 0, 0, 0).ToLocalTime ();
		
		static ZeitgeistProxy proxy;
		public static ZeitgeistProxy Default {
			get {
				if (proxy == null)
					proxy = new ZeitgeistProxy ();
				return proxy;
			}
		}
		
		const string BusName = "org.gnome.zeitgeist";
		const string PathName = "/org/gnome/zeitgeist";
		
		[Interface (BusName)]
		interface IZeitgeistDaemon : org.freedesktop.DBus.Properties
		{
			IDictionary<string, object>[] GetItems (string[] uris);
			
			IDictionary<string, object>[] FindEvents (int min_timestamp, int max_timestamp, int limit, bool sorting_asc, string mode, IDictionary<string, object>[] filters);
		}
		
		IZeitgeistDaemon zeitgeist;
		
		ZeitgeistProxy ()
		{
			try {
				if (Bus.Session.NameHasOwner (BusName)) {
					zeitgeist = Bus.Session.GetObject<IZeitgeistDaemon> (BusName, new ObjectPath (PathName));
				}
			} catch {
				Console.Error.WriteLine ("Failed to connect to zeitgeist bus");
			}
		}
		
		public static DateTime FromUnixTime (int time)
		{
			long ticks = (time * 10000000) + UnixEpoch.Ticks;
			return new DateTime (ticks);
		}
		
		public static int ToUnixTime (DateTime time)
		{
			return (int) ((time.Ticks - UnixEpoch.Ticks) / 10000000);
		}
		
		public IEnumerable<ZeitgeistResult> FindEvents (DateTime start, DateTime stop, int maxResults, bool ascending, 
			                                            string mode, IEnumerable<ZeitgeistFilter> filters)
		{
			int startTime = ToUnixTime (start);
			int stopTime = ToUnixTime (stop);
			
			IDictionary<string, object>[] results;
			try {
				results = zeitgeist.FindEvents (startTime, stopTime, maxResults, ascending, 
					                            mode, filters.Select (f => f.ToDBusFilter ()).ToArray ());
			} catch {
				yield break;
			}
			
			foreach (IDictionary<string, object> result in results) {
				yield return new ZeitgeistResult (result);
			}
		}
		
		public IEnumerable<string> RelevantFilesForMimeTypes (IEnumerable<string> mimes)
		{
			if (zeitgeist == null)
				yield break;
			
			IDictionary<string, object>[] filter = new Dictionary<string, object>[1];
			filter[0] = new Dictionary<string, object> ();
			filter[0]["mimetypes"] = mimes.ToArray ();
			
			IDictionary<string, object>[] results;
			try {
				results = zeitgeist.FindEvents (ToUnixTime (DateTime.Now.AddDays (-31)), 0, 4, false, "mostused", filter);
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				yield break;
			}
			
				
			foreach (IDictionary<string, object> result in results) {
				if (result.ContainsKey ("uri"))
					yield return result["uri"] as string;
			}
		}
	}
}
