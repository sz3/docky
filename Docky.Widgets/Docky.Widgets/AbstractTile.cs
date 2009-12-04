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

using Mono.Unix;

namespace Docky.Widgets
{


	public abstract class AbstractTile : ITile
	{
		
		event EventHandler FinishedLoading;
		
		event EventHandler ITile.FinishedLoading
		{
			add  {
				lock (FinishedLoading) {
					FinishedLoading += value;
				}
			}
			remove {
				lock (FinishedLoading) {
					FinishedLoading -= value;
				}
			}
		}
		
		protected void OnFinishedLoading ()
		{
			if (FinishedLoading != null)
				FinishedLoading (this, EventArgs.Empty);
		}
		
		public virtual string Icon { get; set; }
		
		public virtual string Name  { get; set; }
		public virtual string Description { get; set; }
		public virtual void OnActiveChanged ()
		{
			
		}
		
		public virtual string SubDescriptionTitle {
			get { return ""; }
		}
		
		public virtual string SubDescriptionText {
			get { return ""; }
		}
		
		public virtual string ButtonStateEnabledText {
			get { return Catalog.GetString ("_Remove"); }
		}
		
		public virtual string ButtonStateDisabledText {
			get { return Catalog.GetString ("_Add"); }
		}
		
		public virtual bool ShowActionButton {
			get { return true; }
		}
		
		public virtual bool Enabled {
			get { return true; }
		}
	}
}
