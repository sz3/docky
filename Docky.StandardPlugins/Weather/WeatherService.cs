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
using System.Linq;

namespace WeatherDocklet
{
	/// <summary>
	/// A service to manage all weather sources.
	/// </summary>
	public class WeatherService
	{
		const string ExtensionPath = "/Docky/WeatherSource";
		
		/// <value>
		/// A <see cref="System.Collections.Generic.Dictionary"/> of all weather sources.
		/// </value>
		public Dictionary<string, AbstractWeatherSource> Sources { get; protected set; }
		
		/// <value>
		/// Returns an <see cref="System.Collections.Generic.IEnumerable"/> of all possible weather sources ordered by Name.
		/// </value>
		public IEnumerable<AbstractWeatherSource> WeatherSources {
			get {
				return Sources.Values.OrderBy (d => d.Name);
			}
		}
		
		/// <value>
		/// Returns an <see cref="System.Collections.Generic.IEnumerable"/> of all possible weather sources.
		/// </value>
		public static IEnumerable<AbstractWeatherSource> MAWeatherSources {
			get {
				yield return GoogleWeatherSource.GetInstance ();
				yield return WeatherChannelWeatherSource.GetInstance ();
				yield return WunderWeatherSource.GetInstance ();
			}
//			get { return AddinManager.GetExtensionObjects (ExtensionPath).OfType<AbstractWeatherSource> (); }
		}
		
		/// <summary>
		/// Constructs and initializes a new WeatherService object.
		/// </summary>
		public WeatherService()
		{
//			AddinManager.AddExtensionNodeHandler (ExtensionPath, HandleWeatherSourcesChanged);
			
			BuildSources ();
		}
		
		void BuildSources ()
		{
			Sources = new Dictionary<string, AbstractWeatherSource> ();
			
			foreach (AbstractWeatherSource aws in MAWeatherSources)
				Sources.Add (aws.Name, aws);
		}
		
		#region IDisposable implementation 
		
		public void Dispose ()
		{
//			AddinManager.RemoveExtensionNodeHandler (ExtensionPath, HandleWeatherSourcesChanged);
		}
		
		#endregion 
	}
}
