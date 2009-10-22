//  
//  Copyright (C) 2009 Robert Dyer
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
using System.Net;
using System.Web;
using System.Xml;

using Mono.Unix;

using Docky.Services;

namespace WeatherDocklet
{
	/// <summary>
	/// Provides a weather source using data from Weather Underground.
	/// </summary>
	public class WunderWeatherSource : AbstractWeatherSource
	{
		/// <value>
		/// A map that maps conditions to icon names.
		/// </value>
		static Dictionary<string, string> image_map = new Dictionary<string, string>();
		/// <value>
		/// A map that maps conditions to icon names.
		/// </value>
		protected override Dictionary<string, string> ImageMap { get { return image_map; } }
		
		public override int ForecastDays { get { return 6; } }
		
		public override string Name {
			get {
				return "Weather Underground";
			}
		}
		
		public override string About {
			get {
				return "Weather data provided by and copyright Weather Underground.  " +
					"This source requires locations to be specified as US Zip Code or 'City, Country'.";
			}
		}
		
		protected override string FeedUrl {
			get {
				return "http://api.wunderground.com/auto/wui/geo/WXCurrentObXML/index.xml?query=" + WeatherController.EncodedCurrentLocation;
			}
		}
		
		/// <value>
		/// The URL for forecast information.
		/// </value>
		protected string FeedForecastUrl {
			get {
				return "http://api.wunderground.com/auto/wui/geo/ForecastXML/index.xml?query=" + WeatherController.EncodedCurrentLocation;
			}
		}
		
		protected override string ForecastUrl {
			get {
				return "http://www.wunderground.com/cgi-bin/findweather/getForecast?query="  + WeatherController.EncodedCurrentLocation + "&hourly=1&yday=";
			}
		}
		
		protected override string SearchUrl {
			get {
				return "http://api.wunderground.com/auto/wui/geo/GeoLookupXML/index.xml?query=";
			}
		}
		
		static WunderWeatherSource () {
			image_map.Add ("clear", "weather-clear");
			image_map.Add ("mostlysunny", "weather");
			image_map.Add ("sunny", "weather-clear");
			
			image_map.Add ("cloudy", "weather-overcast");
			
			image_map.Add ("mostlycloudy", "weather-few-clouds");
			image_map.Add ("partlycloudy", "weather-few-clouds");
			image_map.Add ("partlysunny", "weather-few-clouds");
			
			image_map.Add ("chanceflurries", "weather-snow");
			image_map.Add ("chancesnow", "weather-snow");
			image_map.Add ("flurries", "weather-snow");
			image_map.Add ("snow", "weather-snow");
			
			image_map.Add ("fog", "weather-fog");
			image_map.Add ("hazy", "weather-fog");
			
			image_map.Add ("chancerain", "weather-showers");
			image_map.Add ("rain", "weather-showers");
			
			image_map.Add ("chancesleet", "weather-showers");
			image_map.Add ("sleet", "weather-showers");
			
			image_map.Add ("chancetstorms", "weather-storm");
			image_map.Add ("tstorms", "weather-storm");
			
			// currently unused icons
			//image_map.Add ("", "weather-severe-alert");
			//image_map.Add ("", "weather-showers-scattered");
		}
		
		/// <value>
		/// The singleton instance.
		/// </value>
		static AbstractWeatherSource Instance { get; set; }
		
		/// <summary>
		/// Returns the singleton instance for this class.
		/// </summary>
		/// <returns>
		/// A <see cref="AbstractWeatherSource"/> that is the only instance of this class.
		/// </returns>
		public static AbstractWeatherSource GetInstance ()
		{
			if (Instance == null)
				Instance = new WunderWeatherSource ();
			
			return Instance;
		}
		
		protected override void FetchData ()
		{
			XmlDocument xml = FetchXml (FeedUrl);
			ParseXml (xml);
			
			xml = FetchXml (FeedForecastUrl);
			ParseXmlForecast (xml);
		}
		
		protected override void ParseXml (XmlDocument xml)
		{
			XmlNodeList nodelist = xml.SelectNodes ("current_observation/display_location");
			XmlNode item = nodelist.Item (0);
			City = item.SelectSingleNode ("city").InnerText;
			double dbl;
			Double.TryParse (item.SelectSingleNode ("latitude").InnerText, out dbl);
			Latitude = dbl;
			Double.TryParse (item.SelectSingleNode ("longitude").InnerText, out dbl);
			Longitude = dbl;
			SunRise = WeatherController.Sunrise(Latitude, Longitude);
			SunSet = WeatherController.Sunset(Latitude, Longitude);
			
			nodelist = xml.SelectNodes ("current_observation");
			item = nodelist.Item (0);
			
			int temp;
			Int32.TryParse (item.SelectSingleNode ("temp_f").InnerText, out temp);
			Temp = temp;

			if (!item.SelectSingleNode ("heat_index_f").InnerText.Equals ("NA")) {
				Int32.TryParse (item.SelectSingleNode ("heat_index_f").InnerText, out temp);
				FeelsLike = temp;
			} else if (!item.SelectSingleNode ("windchill_f").InnerText.Equals ("NA")) {
				Int32.TryParse (item.SelectSingleNode ("windchill_f").InnerText, out temp);
				FeelsLike = temp;
			} else
				FeelsLike = Temp;
			
			Int32.TryParse (item.SelectSingleNode ("wind_mph").InnerText, out temp);
			Wind = temp;
			WindDirection = item.SelectSingleNode ("wind_dir").InnerText;
			
			Humidity = item.SelectSingleNode ("relative_humidity").InnerText;
			
			Condition = item.SelectSingleNode ("weather").InnerText;
			Image = GetImage (item.SelectSingleNode ("icon").InnerText, true);
		}
		
		/// <summary>
		/// Parses an <see cref="XmlDocument"/> and retrieves forecast information from it.
		/// </summary>
		/// <param name="xml">
		/// A <see cref="XmlDocument"/> containing forecast information.
		/// </param>
		protected void ParseXmlForecast (XmlDocument xml)
		{
			XmlNodeList nodelist = xml.SelectNodes ("forecast/simpleforecast/forecastday");
			
			for (int i = 0; i < ForecastDays; i++)
			{
				XmlNode item = nodelist.Item (i);
				if (item == null)
					break;
				
				Int32.TryParse (item.SelectSingleNode ("high").SelectSingleNode ("fahrenheit").InnerText, out Forecasts [i].high);
				Int32.TryParse (item.SelectSingleNode ("low").SelectSingleNode ("fahrenheit").InnerText, out Forecasts [i].low);
				Forecasts [i].condition = item.SelectSingleNode ("conditions").InnerText;
				Forecasts [i].dow = item.SelectSingleNode ("date").SelectSingleNode ("weekday").InnerText;
				Forecasts [i].dow = Forecasts [i].dow.Substring (0, 3);
				Forecasts [i].image = GetImage (item.SelectSingleNode ("icon").InnerText, false);
				Forecasts [i].chanceOf = Forecasts [i].condition.ToLower ().IndexOf ("chance") != -1;
			}
		}
		
		public override void ShowForecast (int day)
		{
			DateTime forecast = DateTime.Today.AddDays(day);
			DockServices.System.Open (ForecastUrl + (forecast.DayOfYear - 1) + "&weekday=" + forecast.DayOfWeek.ToString());
		}
		
		protected override void ShowRadar (string location)
		{
			DockServices.System.Open ("http://www.wunderground.com/wundermap/?lat=" + Latitude + "&lon=" + Longitude +
										 "&zoom=7&type=map&units=" + (WeatherPreferences.Metric ? "metric" : "english") +
										 "&rad=1&rad.num=1&rad.spd=25&rad.opa=75&rad.stm=0&rad.type=N0R&rad.smo=1&rad.mrg=0&wxsn=0&svr=1&svr.opa=70&cams=0&sat=0&riv=0&mm=0&hur=0&fire=0&tor=0&ndfd=0&pix=0");
		}
		
		public override IEnumerable<string> SearchLocation (string origLocation)
		{
			XmlDocument xml;
			string location = HttpUtility.UrlEncode (origLocation);
			
			try {
				xml = FetchXml (SearchUrl + location);
			} catch (Exception) { yield break; }
			
			if (xml.SelectNodes ("wui_error").Count > 0)
				yield break;
			
			XmlNodeList nodelist = xml.SelectNodes ("locations/location");
			if (nodelist.Count > 0)
			{
				for (int i = 0; i < nodelist.Count; i++)
				{
					yield return nodelist.Item (i).SelectSingleNode ("name").InnerText;
					yield return nodelist.Item (i).SelectSingleNode ("name").InnerText;
				}
			}
			else
			{
				nodelist = xml.SelectNodes ("location");
				
				string city = nodelist.Item (0).SelectSingleNode ("city").InnerText;
				string state = nodelist.Item (0).SelectSingleNode ("state").InnerText;
				string country = nodelist.Item (0).SelectSingleNode ("country").InnerText;
				
				string loc = city;
				if (state.Length > 0)
					loc += ", " + state;
				if (country.Length > 0)
					loc += ", " + country;
				
				if (nodelist.Item (0).Attributes ["type"].InnerText.Equals ("CITY"))
				{
					yield return loc;
					yield return nodelist.Item (0).SelectSingleNode ("zip").InnerText;
				}
				else
				{
					yield return loc;
					yield return origLocation;
				}
			}
			
			yield break;
		}
	}
}
