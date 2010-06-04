//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
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
using Gdk;

namespace Docky.Services
{
	public class HelperMetadata
	{
		const string NameTag = "NAME=";
		const string DescTag = "DESCRIPTION=";
		const string IconTag = "ICON=";
		const string AppUriTag = "APPURI=";
		
		public string Name { get; private set; }
		public string Description { get; private set; }
		public Pixbuf Icon { get; private set; }
		public File IconFile { get; private set; }
		public string AppUri { get; private set; }
		public File DataFile { get; private set; }
		
		public event EventHandler DataReady;

		public HelperMetadata (File dataFile)
		{
			DataFile = dataFile;
			IconFile = null;
			dataFile.ReadAsync (0, null, DataRead);
		}
		
		void OnDataReady ()
		{
			if (DataReady != null)
				DataReady (this, null);
		}
		
		void DataRead (GLib.Object obj, GLib.AsyncResult res) 
		{
			File file = FileAdapter.GetObject (obj);

			using (DataInputStream stream = new DataInputStream (file.ReadFinish (res))) {
				ulong len;
				string line;
				while ((line = stream.ReadLine (out len, null)) != null) {
					int dataStart = line.IndexOf ("\"") + 1;
					int dataEnd = line.LastIndexOf ("\"");
					string data = line.Substring (dataStart, dataEnd - dataStart);
					
					if (line.StartsWith (NameTag)) {
						Name = data;
					} else if (line.StartsWith (DescTag)) {
						Description = data;
					} else if (line.StartsWith (AppUriTag)) {
						AppUri = data;
					} else if (line.StartsWith (IconTag)) {
						if (data.StartsWith ("./")) {
							IconFile = file.Parent.GetChild (data.Substring (2));
							if (IconFile.Exists)
								Icon = DockServices.Drawing.LoadIcon (IconFile.Path + ";;extension");
						} else {
							Icon = DockServices.Drawing.LoadIcon (data + ";;extension", 128);
						}
					}
				}
			}
			OnDataReady ();
		}
	}
}
