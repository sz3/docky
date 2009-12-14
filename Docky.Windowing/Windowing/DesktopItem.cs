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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Docky.Services;

namespace Docky.Windowing
{


	public class DesktopItem : IDisposable
	{
		static string[] LocaleEnvVariables = new [] {"LC_ALL", "LC_MESSAGES", "LANG", "LANGUAGE"};
		
		public string Location { get; private set; }
		
		public string DesktopID {
			get {
				return Path.GetFileNameWithoutExtension (Location);
			}
		}
		
		public DesktopItem (string path)
		{
			Location = path;
		}
		
		public bool HasAttribute (string key)
		{
			if (!File.Exists (Location))
				return false;
			
			StreamReader reader;
			try {
				reader = new StreamReader (Location);
			} catch (Exception e) {
				Log<DesktopItem>.Error (e.Message);
				return false;
			}
			
			Regex regex = new Regex ("^" + key + "\\s*=\\s*");
			
			bool result = false;
			string line;
			while (!reader.EndOfStream) {
				line = reader.ReadLine ();
				
				if (regex.IsMatch (line)) {
					result = true;
					break;
				}
			}
			
			reader.Dispose ();
			
			return result;
		}
		
		public string GetString (string key)
		{
			if (!File.Exists (Location))
				return null;
			
			StreamReader reader;
			try {
				reader = new StreamReader (Location);
			} catch (Exception e) {
				Log<DesktopItem>.Error (e.Message);
				return null;
			}
			
			key = Regex.Escape (key);
			Regex regex = new Regex ("^" + key + "\\s*=\\s*");
			
			string result = null;
			string line;
			while (!reader.EndOfStream) {
				line = reader.ReadLine ();
				
				if (regex.IsMatch (line)) {
					Match match = regex.Matches (line)[0];
					result = line.Remove (match.Index, match.Length);
					break;
				}
			}
			
			reader.Dispose ();
			
			return result;
		}
		
		IEnumerable<string> PostfixStringsForLocale (string locale)
		{
			if (string.IsNullOrEmpty (locale) || locale.Length < 2)
				yield break;
			
			if (locale.Contains (".")) {
				locale = Regex.Replace (locale, @"\..+(?<end>@*)", "${end}");
			}
			yield return locale;
			
			if (locale.Contains ("@")) {
				string noMod = Regex.Replace (locale, @"@*", "");
				yield return noMod;
			}
			
			if (locale.Contains ("_")) {
				string noCountry = Regex.Replace (locale, @"_..", "");
				yield return noCountry;
			}
			
			yield return locale.Substring (0, 2);
		}
		
		public string GetLocaleString (string key)
		{
			string locale = null;
			
			foreach (string env in LocaleEnvVariables) {
				locale = Environment.GetEnvironmentVariable (env);
				if (!string.IsNullOrEmpty (locale) && locale.Length >= 2)
					break;
			}
			
			// short circuit out of here, we cant find locale
			if (string.IsNullOrEmpty (locale) || locale.Length < 2)
				return GetString (key);
			
			string result = null;
			
			foreach (string postfix in PostfixStringsForLocale (locale)) {
				result = GetString (string.Format ("{0}[{1}]", key, postfix));
				if (result != null)
					return result;
			}
			
			return GetString (key);
		}
		
		public IEnumerable<string> GetStrings (string key)
		{
			string result = GetString (key);
			if (result == null)
				return Enumerable.Empty<string> ();
			
			return result.Split (';');
		}
		
		public bool GetBool (string key)
		{
			string result = GetString (key);
			
			if (string.Equals (result, "false", StringComparison.CurrentCultureIgnoreCase)) {
				return false;
			} else if (string.Equals (result, "true", StringComparison.CurrentCultureIgnoreCase)) {
				return true;
			} else {
				throw new ArgumentException ();
			}
		}
		
		public double GetDouble (string key)
		{
			string result = GetString (key);
			
			return Convert.ToDouble (result);
		}
		
		public void Launch (IEnumerable<string> uris)
		{
			GLib.DesktopAppInfo dai = GLib.DesktopAppInfo.NewFromFilename (Location);
			
			string[] uriList = uris.Where (uri => uri != null).ToArray ();
			if (!uriList.Any ()) {
				dai.Launch (null, null);
				return;
			} else {
				if (dai.SupportsUris) {
					GLib.List glist = new GLib.List (uriList as object[], typeof(string), false, true);
					dai.LaunchUris (glist, null);
					glist.Dispose ();
				} else if (dai.SupportsFiles) {
					GLib.File[] files = uriList.Select (uri => GLib.FileFactory.NewForUri (uri)).ToArray ();
					GLib.List glist = new GLib.List (files as object[], typeof(GLib.File), false, true);
					dai.Launch (glist, null);
					glist.Dispose ();
				}
			}
			
			dai.Dispose ();
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
		}
		#endregion

	}
}
