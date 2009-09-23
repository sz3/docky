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
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Gtk;

using Docky.Items;

namespace Docky.Interface
{


	public interface IDockPreferences
	{
		event EventHandler PositionChanged;
		event EventHandler IconSizeChanged;
		event EventHandler AutohideChanged;
		event EventHandler FadeOnHideChanged;
		event EventHandler FadeOpacityChanged;
		event EventHandler ZoomEnabledChanged;
		event EventHandler ZoomPercentChanged;
		
		event EventHandler<ItemProvidersChangedEventArgs> ItemProvidersChanged;
		
		FileApplicationProvider DefaultProvider { get; set; }
		
		IEnumerable<IDockItemProvider> ItemProviders { get; }
		
		AutohideType Autohide { get; set; }
		
		bool FadeOnHide { get; set; }
		
		double FadeOpacity { get; set; }
		
		DockPosition Position { get; set; }
		
		int IconSize { get; set; }
		
		bool ZoomEnabled { get; set; }
				
		double ZoomPercent { get; set; }
		
		bool SetName (string name);
		
		string GetName ();
		
		void SyncPreferences ();
	}
}
