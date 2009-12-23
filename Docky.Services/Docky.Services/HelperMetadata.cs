//  
//  Copyright (C) 2009 Chris Szikszoy
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
		
		public string Name { get; private set; }
		public string Description { get; private set; }
		public Pixbuf Icon { get; private set; }

		public HelperMetadata (File dataFile)
		{
			dataFile.ReadAsync (0, null, DataReady);
		}
		
		void DataReady (GLib.Object obj, GLib.AsyncResult res) 
		{
			File file = FileAdapter.GetObject (obj);

			using (DataInputStream stream = new DataInputStream (file.ReadFinish (res))) {
				ulong len;
				string line;
				while ((line = stream.ReadLine (out len, null)) != null) {
					int dataStart = line.IndexOf ("\"") + 1;
					int dataEnd = line.LastIndexOf ("\"");
					string data = line.Substring (dataStart, dataEnd - dataStart);
					
					if (line.StartsWith (NameTag))
						Name = data;
					else if (line.StartsWith (DescTag))
						Description = data;
					else if (line.StartsWith (IconTag)) {
						if (data.StartsWith ("./"))
							Icon = DockServices.Drawing.LoadIcon (file.Parent.GetChild (data.Substring (2)).Path);
						else
							Icon = DockServices.Drawing.LoadIcon (data, 128);
					}
				}
			}
		}
	}
}
