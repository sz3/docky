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

namespace Docky.Widgets
{

	public interface ITile
	{
		event EventHandler FinishedLoading;
		
		string Icon { get; }
		string Name { get; }
		string Description { get; }
		string SubDescriptionTitle { get; }
		string SubDescriptionText { get; }
		string ButtonStateEnabledText { get; }
		string ButtonStateDisabledText { get; }
		bool ShowActionButton { get; }
		bool Enabled { get; }
		
		void OnActiveChanged ();
	}
}
