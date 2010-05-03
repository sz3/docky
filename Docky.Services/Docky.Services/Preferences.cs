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
using System.Text.RegularExpressions;
using System.Threading;

using GConf;
using Gnome.Keyring;
using Mono.Unix;

namespace Docky.Services
{
	public class Preferences<TOwner> : IPreferences
		where TOwner : class
	{
		#region IPreferences - based on GConf
		static Regex nameRegex = new Regex ("[^a-zA-Z0-9]");
		static Client client = new Client ();
		
		readonly string GConfPrefix = "/apps/docky-2/" + typeof (TOwner).FullName.Replace (".", "/");
		
		public T Get<T> (string key, T def)
		{
			object result;
			try {
				result = client.Get (AbsolutePathForKey (key, GConfPrefix));
			} catch (GConf.NoSuchKeyException) {
				Log<Preferences<TOwner>>.Debug ("Key {0} does not exist, creating.", key);
				Set<T> (key, def);
				return def;
			} catch (Exception e) {
				Log<Preferences<TOwner>>.Error ("Failed to get gconf value for {0} : '{1}'", key, e.Message);
				Log<Preferences<TOwner>>.Info (e.StackTrace);
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
				Log<Preferences<TOwner>>.Error ("Encountered error setting GConf key {0}: '{1}'", key, e.Message);
				Log<Preferences<TOwner>>.Info (e.StackTrace);
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
		
		public string SanitizeKey (string key)
		{
			return nameRegex.Replace (key, "_");
		}
		
		public void AddNotify (string path, NotifyEventHandler handler)
		{
			try {
				client.AddNotify (path, handler);
			} catch (Exception e) {
				Log<Preferences<TOwner>>.Error ("Error removing notification handler, {0}", e.Message);
				Log<Preferences<TOwner>>.Debug (e.StackTrace);
			}
		}
		
		public void RemoveNotify (string path, NotifyEventHandler handler)
		{
			try {
				client.RemoveNotify (path, handler);
			} catch (Exception e) {
				Log<Preferences<TOwner>>.Error ("Error removing notification handler, {0}", e.Message);
				Log<Preferences<TOwner>>.Debug (e.StackTrace);
			}
		}

		#endregion
		
		#region IPreferences - secure, based on Gnome Keyring
		
		AutoResetEvent autoEvent = new AutoResetEvent(false);
		
		readonly string ErrorSavingMessage = "Error saving {0} : '{0}'";
		readonly string KeyNotFoundMessage = "Key \"{0}\" not found in keyring";
		readonly string KeyringUnavailableMessage = "gnome-keyring-daemon could not be reached!";
		
		readonly string GnomeKeyringPrefix = "docky-2/" + typeof (TOwner).FullName.Replace (".", "/");

		public bool SetSecure<T> (string key, T val)
		{
			if (typeof (T) != typeof (string))
				throw new NotImplementedException ("Unimplemented for non string values");
			
			bool success = false;
			
			lock (autoEvent) {
				DockServices.System.RunOnMainThread (() => {
					try {
						if (!Ring.Available) {
							Log<Preferences<TOwner>>.Error (KeyringUnavailableMessage);
							return;
						}
						
						Hashtable keyData = new Hashtable ();
						keyData [AbsolutePathForKey (key, GnomeKeyringPrefix)] = key;
					
						Ring.CreateItem (Ring.GetDefaultKeyring (), ItemType.GenericSecret, AbsolutePathForKey (key, GnomeKeyringPrefix), keyData, val.ToString (), true);
						success = true;
					} catch (KeyringException e) {
						Log<Preferences<TOwner>>.Error (ErrorSavingMessage, key, e.Message);
						Log<Preferences<TOwner>>.Info (e.StackTrace);
					} finally {
						autoEvent.Set ();
					}
				});
				
				autoEvent.WaitOne (1000);
			}
			
			return success;
		}

		public T GetSecure<T> (string key, T def)
		{
			T val = def;
			
			lock (autoEvent) {
				DockServices.System.RunOnMainThread (() => {
					try {
						if (!Ring.Available) {
							Log<Preferences<TOwner>>.Error (KeyringUnavailableMessage);
							return;
						}
						
						Hashtable keyData = new Hashtable ();
						keyData [AbsolutePathForKey (key, GnomeKeyringPrefix)] = key;
						
						foreach (ItemData item in Ring.Find (ItemType.GenericSecret, keyData)) {
							if (!item.Attributes.ContainsKey (AbsolutePathForKey (key, GnomeKeyringPrefix))) continue;

							val = (T) Convert.ChangeType (item.Secret, typeof (T));
							return;
						}
					} catch (KeyringException e) {
						Log<Preferences<TOwner>>.Error (KeyNotFoundMessage, AbsolutePathForKey (key, GnomeKeyringPrefix), e.Message);
						Log<Preferences<TOwner>>.Info (e.StackTrace);
					} finally {
						autoEvent.Set ();
					}
				});
				
				autoEvent.WaitOne (1000);
			}
			
			return val;
		}
		
		#endregion
	}
}
