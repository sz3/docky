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


	public class ZeitgeistFilter
	{
		public List<string> Names { get; private set; }

		public List<string> Uris { get; private set; }
		
		public List<string> Tags { get; private set; }
		
		public List<string> MimeTypes { get; private set; }
		
		public List<string> Sources { get; private set; }
		
		public List<string> Content { get; private set; }
		
		public List<string> Apps { get; private set; }
		
		public bool Bookmarked { get; set; }
		
		public ZeitgeistFilter ()
		{
			Names = new List<string> ();
			Uris = new List<string> ();
			Tags = new List<string> ();
			MimeTypes = new List<string> ();
			Sources = new List<string> ();
			Content = new List<string> ();
			Apps = new List<string> ();
			Bookmarked = false;
		}
		
		internal IDictionary<string, object> ToDBusFilter ()
		{
			IDictionary<string, object> result = new Dictionary<string, object> ();
			result["name"] = Names.ToArray ();
			result["uri"] = Uris.ToArray ();
			result["tags"] = Tags.ToArray ();
			result["mimetypes"] = MimeTypes.ToArray ();
			result["source"] = Sources.ToArray ();
			result["content"] = Content.ToArray ();
			result["applicaiton"] = Apps.ToArray ();
			result["bookmarked"] = Bookmarked;
			
			return result;
		}
	}
}
