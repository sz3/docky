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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

using GConf;
using Gnome.Keyring;
using Mono.Unix;

namespace Docky.Services
{
	public class Preferences<TOwner> : IPreferences
		where TOwner : class
	{
		#region IPreferences - based on GConf
		
		static Client client = new Client ();
		
		readonly string GConfPrefix = "/apps/docky-2/" + typeof (TOwner).FullName.Replace (".", "/");
		
		public T Get<T> (string key, T def)
		{
			object result;
			try {
				result = client.Get (AbsolutePathForKey (key, GConfPrefix));
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
				client.Set (AbsolutePathForKey (key, GConfPrefix), val);
			} catch (Exception e) {
				Console.Error.WriteLine ("Encountered error setting GConf key {0}: {1}", key, e.Message);
				Console.Error.WriteLine (e.StackTrace);
				success = false;
			}
			return success;
		}
		
		string AbsolutePathForKey (string key, string prefix)
		{
			if (key.StartsWith ("/"))
				return key;
			return string.Format ("{0}/{1}", prefix, key);
		}
		
		#endregion
		
		#region IPreferences - secure, based on Gnome Keyring
		
		readonly string ErrorSavingMessage = Catalog.GetString ("Error saving {0}");
		readonly string KeyNotFoundMessage = Catalog.GetString ("Key \"{0}\" not found in keyring");
		readonly string KeyringUnavailableMessage = Catalog.GetString ("gnome-keyring-daemon could not be reached!");
		
		const string DefaultRootPath = "gnome-do";

		public bool SetSecure<T> (string key, T val)
		{
			if (typeof (T) != typeof (string))
				throw new NotImplementedException ("Unimplemented for non string values");

			Hashtable keyData;
			
			if (!Ring.Available) {
				//Log.Error (KeyringUnavailableMessage);
				return false;
			}

			keyData = new Hashtable ();
			keyData [AbsolutePathForKey (key, DefaultRootPath)] = key;
			
			try {
				Ring.CreateItem (Ring.GetDefaultKeyring (), ItemType.GenericSecret, AbsolutePathForKey (key, DefaultRootPath), keyData, val.ToString (), true);
			} catch (KeyringException e) {
				//Log.Error (ErrorSavingMessage, key, e.Message);
				//Log.Debug (e.StackTrace);
				return false;
			}

			return true;
		}

		public T GetSecure<T> (string key, T def)
		{
			Hashtable keyData;
			
			if (!Ring.Available) {
				//Log.Error (KeyringUnavailableMessage);
				return def;
			}

			keyData = new Hashtable ();
			keyData [AbsolutePathForKey (key, DefaultRootPath)] = key;
			
			try {
				foreach (ItemData item in Ring.Find (ItemType.GenericSecret, keyData)) {
					if (!item.Attributes.ContainsKey (AbsolutePathForKey (key, DefaultRootPath))) continue;

					string secureValue = item.Secret;
					return (T) Convert.ChangeType (secureValue, typeof (T));
				}
			} catch (KeyringException) {
				//Log.Debug (KeyNotFoundMessage, AbsolutePathForKey (key));
			}

			return def;
		}
		#endregion
	}
}
