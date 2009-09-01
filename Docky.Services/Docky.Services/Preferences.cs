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
using System.IO;
using System.Linq;
using System.Text;

namespace Docky.Services
{


	public class Preferences<TOwner> : IPreferences
		where TOwner : class
	{
		static GConf.Client client = new GConf.Client ();
		
		readonly string GConfPrefix = "/apps/docky-2/preferences/" + typeof (TOwner).FullName.Replace (".", "/");
		
		public T Get<T> (string key, T def)
		{
			object result;
			try {
				result = client.Get (AbsolutePathForKey (key));
			} catch (GConf.NoSuchKeyException e) {
				Console.Error.WriteLine (e.Message);
				Set<T> (key, def);
				return def;
			} catch (Exception e) {
				Console.Error.WriteLine (e.Message);
				Console.Error.WriteLine (e.StackTrace);
				return def;
			}
			
			if (result != null && result is T)
				return (T) result;
			
			return def;
		}
		
		public bool Set<T> (string key, T val)
		{
			bool success = true;
			try {
				client.Set (AbsolutePathForKey (key), val);
			} catch (Exception e) {
				Console.Error.WriteLine ("Encountered error setting GConf key {0}: {1}", key, e.Message);
				Console.Error.WriteLine (e.StackTrace);
				success = false;
			}
			return success;
		}
		
		string AbsolutePathForKey (string key)
		{
			if (key.StartsWith ("/"))
				return key;
			return string.Format ("{0}/{1}", GConfPrefix, key);
		}
	}
}
