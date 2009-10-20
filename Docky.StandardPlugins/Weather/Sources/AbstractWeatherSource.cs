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
using System.Threading;
using System.Web;
using System.Xml;

using Mono.Unix;

using Docky.Services;

namespace WeatherDocklet
{
	/// <summary>
	/// The base class for all weather sources.
	/// </summary>
	public abstract class AbstractWeatherSource : IWeatherSource
	{
		/// <value>
		/// A map that maps conditions to icon names.
		/// </value>
		protected abstract Dictionary<string, string> ImageMap { get; }

		#region IWeatherSource implementation

		public abstract int ForecastDays { get; }

		public string City { get; protected set; }
		
		public double Latitude { get; protected set; }
		public double Longitude { get; protected set; }
		
		public DateTime SunRise { get; protected set; }
		public DateTime SunSet { get; protected set; }
		
		public int Temp { get; protected set; }
		public int FeelsLike { get; protected set; }
		public string Condition { get; protected set; }
		public int Wind { get; protected set; }
		public string WindDirection { get; protected set; }
		public string Humidity { get; protected set; }
		public string Image { get; protected set; }
		
		public WeatherForecast[] Forecasts { get; protected set; }
		
		public abstract string Name { get; }
		public abstract string About { get; }
		
		public bool SupportsFeelsLike { get { return Temp != FeelsLike; } }
		
		public event Action WeatherReloading;
		public event EventHandler<WeatherErrorArgs> WeatherError;
		public event Action WeatherUpdated;
		
		public void ReloadWeatherData ()
		{
			new Thread(() => {
				Log<AbstractWeatherSource>.Info (Name + ": Reloading weather data");
				
				try {
					OnWeatherReloading ();
	
					FetchData ();
					
					if (WeatherPreferences.Metric)
						ConvertResults ();
					
					OnWeatherUpdated ();
				} catch (NullReferenceException) {
					OnWeatherError (Catalog.GetString ("Invalid Weather Location"));
					Log<AbstractWeatherSource>.Info (Name + ": Invalid Weather Location");
				} catch (XmlException) {
					OnWeatherError (Catalog.GetString ("Invalid Weather Location"));
					Log<AbstractWeatherSource>.Info (Name + ": Invalid Weather Location");
				} catch (WebException e) {
					OnWeatherError (Catalog.GetString ("Network Error: " + e.Message));
					Log<AbstractWeatherSource>.Info (Name + ": Network Error: " + e.Message);
				} catch (Exception e) {
					OnWeatherError (Catalog.GetString ("Invalid Weather Location"));
					Log<AbstractWeatherSource>.Error (Name + ": " + e.ToString ());
				}
			}).Start ();
		}
		
		public void ShowRadar ()
		{
			ShowRadar (WeatherController.EncodedCurrentLocation);
		}
		
		public virtual void ShowForecast (int day)
		{
			DockServices.System.Open (ForecastUrl + day);
		}
		
		public bool IsNight ()
		{
			return (DateTime.Now < SunRise || DateTime.Now > SunSet);
		}
		
		public abstract IEnumerable<string> SearchLocation (string location);
		
		#endregion
		
		/// <summary>
		/// Creates a new weather source object.
		/// </summary>
		protected AbstractWeatherSource ()
		{
			Image = DefaultImage;
			Forecasts = new WeatherForecast [ForecastDays];
			for (int i = 0; i < ForecastDays; i++)
				Forecasts [i].image = DefaultImage;
		}
		
		/// <value>
		/// The URL to retrieve weather data from.
		/// </value>
		protected abstract string FeedUrl { get; }
		
		/// <value>
		/// The URL to display a day's forecast.
		/// </value>
		protected abstract string ForecastUrl { get; }
		
		/// <value>
		/// A URL for searching for Location's.
		/// </value>
		protected abstract string SearchUrl { get; }
		
		/// <value>
		/// The default image name.
		/// </value>
		protected static string DefaultImage {
			get {
				return Gtk.Stock.DialogQuestion;
			}
		}
		
		/// <summary>
		/// Finds an icon name for the specified condition.  Attempts to guess if the map does
		/// not contain an entry for the condition.  Also attempts to use night icons when appropriate.
		/// </summary>
		/// <param name="condition">
		/// A <see cref="System.String"/> representing the condition to look up an icon for.
		/// </param>
		/// <param name="useNight">
		/// A <see cref="System.Boolean"/> indicating if night icons should be used (if it is night).
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> representing the icon name for the condition.
		/// </returns>
		protected string GetImage (string condition, bool useNight)
		{
			condition = condition.ToLower ();
			
			if (!ImageMap.ContainsKey (condition))
			{
				Log<AbstractWeatherSource>.Info (Name + ": no image for condition '" + condition + "'");
				
				if (condition.Contains ("sun"))
				{
					if (useNight && IsNight ())
						return "weather-clear-night";
					else
						return "weather-clear";
				}
				if (condition.Contains ("storm") || condition.Contains ("thunder"))
					return "weather-storm";
				if (condition.Contains ("rain") || condition.Contains ("showers"))
					return "weather-showers";
				if (condition.Contains ("drizzle") || condition.Contains ("mist"))
					return "weather-showers-scattered";
				if (condition.Contains ("snow") || condition.Contains ("flur"))
					return "weather-snow";
				if (condition.Contains ("cloud"))
				{
					if (useNight && IsNight ())
						return "weather-few-clouds-night";
					else
						return "weather-few-clouds";
				}
				if (condition.Contains ("fog"))
					return "weather-fog";
				
				return DefaultImage;
			}
			
			if (useNight && IsNight ())
			{
				if (ImageMap [condition].Equals ("weather-clear"))
					return "weather-clear-night";
				if (ImageMap [condition].Equals ("weather-few-clouds"))
					return "weather-few-clouds-night";
			}
			
			return ImageMap [condition];
		}
		
		/// <summary>
		/// Gets the XML document and parses it.
		/// </summary>
		protected virtual void FetchData ()
		{
			XmlDocument xml = FetchXml (FeedUrl);
			ParseXml (xml);
		}
		
		/// <summary>
		/// Retrieves an XML document from the specified URL.
		/// </summary>
		/// <param name="url">
		/// A <see cref="System.String"/> representing the URL to retrieve.
		/// </param>
		/// <returns>
		/// A <see cref="XmlDocument"/> from the URL.
		/// </returns>
		protected XmlDocument FetchXml (string url)
		{
			Log<AbstractWeatherSource>.Debug (Name + ": Fetching XML file '" + url + "'");
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create (url);
			request.UserAgent = @"Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.10) Gecko/2009042523 Ubuntu/9.04 (jaunty) Firefox/3.0.10";
			
			XmlDocument xml = new XmlDocument ();
			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ())
				try {
					xml.Load (response.GetResponseStream ());
				} finally {
					response.Close ();
				}
			
			return xml;
		}
		
		/// <summary>
		/// Parses the <see cref="XmlDocument"/> to obtain weather data.
		/// </summary>
		/// <param name="xml">
		/// A <see cref="XmlDocument"/> containing the weather data.
		/// </param>
		protected abstract void ParseXml (XmlDocument xml);
		
		/// <summary>
		/// Shows the radar in the default browser.
		/// </summary>
		/// <param name="location">
		/// A <see cref="System.String"/> representing the Location to show a radar for.
		/// </param>
		protected abstract void ShowRadar (string location);
		
		/// <summary>
		/// Forwards the weather source event.
		/// </summary>
		protected void OnWeatherUpdated ()
		{
			if (WeatherUpdated != null)
				Gtk.Application.Invoke (delegate { WeatherUpdated(); });
		}
		
		/// <summary>
		/// Forwards the weather source event.
		/// </summary>
		protected void OnWeatherError (string msg)
		{
			if (WeatherError != null)
				Gtk.Application.Invoke (delegate { WeatherError (this, new WeatherErrorArgs(msg)); });
		}
		
		/// <summary>
		/// Forwards the weather source event.
		/// </summary>
		protected void OnWeatherReloading ()
		{
			if (WeatherReloading != null)
				Gtk.Application.Invoke (delegate { WeatherReloading (); });
		}
		
		/// <summary>
		/// If results need to be in metric, converts them to metric.
		/// </summary>
		protected void ConvertResults ()
		{
			Temp = WeatherUnits.ConvertFtoC (Temp);
			FeelsLike = WeatherUnits.ConvertFtoC (FeelsLike);
			Wind = WeatherUnits.ConvertMphToKmh (Wind);
			
			for (int i = 0; i < ForecastDays; i++)
			{
				Forecasts [i].high = WeatherUnits.ConvertFtoC (Forecasts [i].high);
				Forecasts [i].low = WeatherUnits.ConvertFtoC (Forecasts [i].low);
			}
		}
	}
}
