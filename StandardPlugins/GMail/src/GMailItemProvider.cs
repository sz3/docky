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

using Docky.Items;

namespace GMail
{
	public class GMailItemProvider : AbstractDockItemProvider
	{
		
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "GMail";
			}
		}
		
		public override void Dispose ()
		{
			string[] keys = new string [items.Keys.Count];
			items.Keys.CopyTo (keys, 0);
			foreach (string label in keys)
				RemoveItem (label);
		}
		
		#endregion
		
		public void ItemVisibilityChanged (AbstractDockItem item, bool newVisible)
		{
			if (visible[item] == newVisible)
				return;
			
			visible[item] = newVisible;

			SetItems ();
		}
		
		void SetItems ()
		{
			Items = items.Values
				.Where (adi => (adi is GMailDockItem) && (adi as GMailDockItem).Visible)
				.Cast<AbstractDockItem> ();
		}
		
		void RemoveItem (string label)
		{
			if (!items.ContainsKey (label))
				return;

			AbstractDockItem item = items[label];
			items.Remove (label);
			visible.Remove (item);
			
			SetItems ();
			
			item.Dispose ();
		}
		
		void AddItem (string label)
		{
			if (items.ContainsKey (label))
				return;

			GMailDockItem item = new GMailDockItem (label);
			item.Owner = this;
			
			items.Add (label, item);
			visible.Add (item, item.Visible);
			
			SetItems ();
		}

		Dictionary<string, AbstractDockItem> items = new Dictionary<string, AbstractDockItem> ();
		Dictionary<AbstractDockItem, bool> visible = new Dictionary<AbstractDockItem, bool> ();
		
		public GMailItemProvider ()
		{
			AddItem ("Inbox");
			
			foreach (string label in GMailPreferences.Labels)
				AddItem (label);
			
			GMailPreferences.LabelsChanged += HandleLabelsChanged;
			
			SetItems ();
		}
		
		void HandleLabelsChanged (object o, EventArgs e)
		{
			string[] keys = new string [items.Keys.Count];
			items.Keys.CopyTo (keys, 0);
			foreach (string label in keys)
				if (label != "Inbox")
					RemoveItem (label);

			foreach (string label in GMailPreferences.Labels)
				AddItem (label);
		}
		
		public override void AddedToDock ()
		{
			GLib.Idle.Add (delegate {
				foreach (GMailDockItem item in items.Values)
					item.Atom.ResetTimer ();
				return false;
			});
		}
	}
}
