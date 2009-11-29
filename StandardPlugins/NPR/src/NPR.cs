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
using System.Web;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using System.Collections.Generic;

using Docky.Services;

namespace NPR
{


	public class NPR
	{
		const string apiKey = "MDA0NDA4MTcxMDEyNTkzNzkwMTc4ODYwYQ001";
		const string stationsUrl = "http://api.npr.org/stations";
		
		static IPreferences prefs;
		
		public static int[] MyStations {
			get {
				return prefs.Get<int []> ("MyStations", null);
			}
			set {
				prefs.Set<int []> ("MyStations", value);
			}
		}
		
		static NPR ()
		{
			prefs = DockServices.Preferences.Get <NPR> ();
			
			MyStations = new int[] {145,233,270,55,1089};
		}
		
		public NPR ()
		{
		}
		
		static string BuildQueryString (string url, NameValueCollection query)
		{
			StringBuilder queryString = new StringBuilder ();
			queryString.AppendFormat ("{0}?",url);
			foreach (string key in query.Keys)
			{
				queryString.AppendFormat ("{0}={1}&", HttpUtility.UrlEncode(key),
				                          HttpUtility.UrlEncode(query[key]));
			}
			queryString.AppendFormat ("apiKey={0}", apiKey);
			Console.WriteLine ("Query string: {0}", queryString.ToString ());
			return queryString.ToString ();
		}
		
		static XElement APIReturn (string url, NameValueCollection query)
		{
			return XElement.Load (BuildQueryString (url, query));
		}
		
		public static IEnumerable<Station> SearchStations (uint zip) 
		{
			NameValueCollection query = new NameValueCollection ();
			query["zip"] = zip.ToString ();	
						
			return APIReturn (stationsUrl, query).Elements ("station").Select (e => new Station (e));
		}
		
		public static Station StationById (int id)
		{
			NameValueCollection query = new NameValueCollection ();
			query["id"] = id.ToString ();
			
			return new Station (APIReturn (stationsUrl, query).Element ("station"));
			
		}
	}
}
