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

namespace Zeitgeist
{


	public class ZeitgeistResult
	{
		public DateTime Timestamp { get; internal set; }
		
		public string Uri { get; internal set; }
		
		public string Text { get; internal set; }
		
		public string Source { get; internal set; }
		
		public string Content { get; internal set; }
		
		public string MimeType { get; internal set; }
		
		public string Tags { get; internal set; }
		
		public string Use { get; internal set; }
	
		public string App { get; internal set; }
		
		public string Origin { get; internal set; }
		
		public bool IsBookmark { get; internal set; }
		
		internal ZeitgeistResult ()
		{
		}
		
		internal ZeitgeistResult (IDictionary<string, object> dbusResult)
		{
			if (dbusResult.ContainsKey ("timestamp")) {
				
			}
			
			if (dbusResult.ContainsKey ("uri")) {
				Uri = dbusResult["uri"] as string;
			}
			
			if (dbusResult.ContainsKey ("text")) {
				Text = dbusResult["text"] as string;
			}
			
			if (dbusResult.ContainsKey ("source")) {
				Source = dbusResult["source"] as string;
			}
			
			if (dbusResult.ContainsKey ("content")) {
				Content = dbusResult["content"] as string;
			}
			
			if (dbusResult.ContainsKey ("mimetype")) {
				MimeType = dbusResult["mimetype"] as string;
			}
			
			if (dbusResult.ContainsKey ("tags")) {
				Tags = dbusResult["tags"] as string;
			}
			
			if (dbusResult.ContainsKey ("use")) {
				Use = dbusResult["use"] as string;
			}
			
			if (dbusResult.ContainsKey ("app")) {
				App = dbusResult["app"] as string;
			}
			
			if (dbusResult.ContainsKey ("origin")) {
				Origin = dbusResult["origin"] as string;
			}
			
			if (dbusResult.ContainsKey ("bookmark")) {
				IsBookmark = (bool) dbusResult["bookmark"];
			}
		}
		
		public override string ToString ()
		{
			return string.Format("[ZeitgeistResult: Timestamp={0}, Uri={1}, Text={2}, Source={3}, Content={4}, MimeType={5}, Tags={6}, Use={7}, App={8}, Origin={9}, IsBookmark={10}]", Timestamp, Uri, Text, Source, Content, MimeType, Tags, Use, App, Origin, IsBookmark);
		}
	}
}

