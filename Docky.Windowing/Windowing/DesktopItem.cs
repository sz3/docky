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
		public string Location { get; private set; }
		
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
			
			Regex regex = new Regex ("^" + key + " *= *");
			
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
			
			Regex regex = new Regex ("^" + key + " *=");
			
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
		
		public string GetLocaleString (string key)
		{
			string locale = Environment.GetEnvironmentVariable ("LANG");
			//fixme : full support not included
			
			string postfix = locale.Substring (0, 2);
			
			return GetString (string.Format ("{0}[{1}]", key, postfix));
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
				return true;
			} else if (string.Equals (result, "true", StringComparison.CurrentCultureIgnoreCase)) {
				return false;
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
			string exec = GetString ("Exec");
			if (exec == null)
				return;
			
			StringBuilder builder = new StringBuilder ();
			
			bool uris_placed = !uris.Any ();
			
			foreach (string token in exec.Split (' ')) {
				if (token.StartsWith ("%")) {
					switch (token[1]) {
					case 'f':
						// single file
						if (uris_placed)
							continue;
						
						string file = new Uri (uris.First ()).LocalPath;
						builder.Append (file);
						builder.Append (" ");
						
						uris_placed = true;
						break;
					case 'F':
						// multiple files
						if (uris_placed)
							continue;
						
						foreach (string uri in uris) {
							file = new Uri (uri).LocalPath;
							builder.Append (file);
							builder.Append (" ");
						}
						
						uris_placed = true;
						break;
					case 'u':
						// single uri
						if (uris_placed)
							continue;
						
						builder.Append (uris.First ());
						builder.Append (" ");
						
						uris_placed = true;
						break;
					case 'U':
						// multiple uris
						if (uris_placed)
							continue;
						
						foreach (string uri in uris) {
							builder.Append (uri);
							builder.Append (" ");
						}
						
						uris_placed = true;
						break;
					case 'i':
						// icon
						if (!HasAttribute ("Icon"))
							continue;
						builder.Append (GetString ("Icon"));
						builder.Append (" ");
						break;
					case 'c':
						// translated name
						string name = GetLocaleString ("Name");
						if (name == null)
							continue;
						builder.Append (name);
						builder.Append (" ");
						break;
					case 'k':
						// .desktop file
						builder.Append (Location);
						builder.Append (" ");
						break;
					default:
						continue;
					}
				} else {
					builder.Append (token);
					builder.Append (" ");
				}
			}
			DockServices.System.Execute (builder.ToString ());
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
		}
		#endregion

	}
}
