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
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Gtk;

using Docky.Services;
using Docky.Items;

namespace Docky.Interface
{
	
	public class AddinTreeNode : Gtk.TreeNode
	{
		public string Icon { 
			get {
				if (Provider == null ||  !PluginManager.AddinFromID (AddinID).Enabled)
					return PluginManager.DefaultPluginIcon;
				// we should enhance Provider to provide an icon  we can use here
				else
					return Provider.Icon;
			}
		}

		public string Name  { get; private set; }

		public string AddinID  { get; private set; }
		
		public AbstractDockItemProvider Provider  { get; set; }

		public AddinTreeNode (string addinID, AbstractDockItemProvider provider)
		{
			AddinID = addinID;
			if (string.IsNullOrEmpty (addinID))
			    AddinID = PluginManager.AddinIDFromProvider (provider);
			
			Name = PluginManager.AddinFromID (AddinID).Name;
			
			Provider = provider;
			if (provider == null)
				Provider = null;
		}
		
		public AddinTreeNode (string addinId) : this (addinId, null)
		{	
		}
		
		public AddinTreeNode (AbstractDockItemProvider provider) : this (null, provider)
		{
		}
	}
	
	public class AddinOrderChangedArgs : EventArgs
	{
		public List<AddinTreeNode> NewOrder { get; private set; }
		
		public AddinOrderChangedArgs (List<AddinTreeNode> newOrder)
		{
			NewOrder = newOrder;
		}
	}
	
	public class AddinTreeView : Gtk.TreeView
	{
		public enum Column {
			Name = 0,
			AddinNode,
			NumColumns,
		}
		
		public event EventHandler<AddinOrderChangedArgs> AddinOrderChanged;
		
		const int IconSize = 22;
		const int IconPadding = 3;
			
		public List<AddinTreeNode> current_order { get; private set; }
		ListStore store;
		
		public AddinTreeView (bool reorderable) : base ()
		{
			CellRenderer cell;
			current_order = new List<AddinTreeNode> ();
			
			Reorderable = reorderable;
			HeadersVisible = false;
			Model = store = new ListStore (typeof (string), typeof (AddinTreeNode));
			
			cell = new CellRendererPixbuf ();
			AppendColumn ("Icon", cell, IconFunction as TreeCellDataFunc);
			
			cell = new CellRendererText ();
			AppendColumn ("Name", cell, "text", Column.Name);
						
			ButtonReleaseEvent += HandleHandleButtonReleaseEvent;
		}

		// There is a GTK# problem with Model.RowsReordered never being emitted
		// this is a workaround to force the event.  To see more about the problem
		// go here: http://www.nabble.com/forum/Search.jtp?forum=1373&local=y&query=RowsReordered
		void HandleHandleButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
		{
			GLib.Timeout.Add (200, delegate {
				UpdateAddinOrder ();
				return false;
			});
		}

		void UpdateAddinOrder ()
		{
			List<AddinTreeNode> new_order = GetAddinOrder ();
			bool changed = false;
			int i=0;
			
			//compare the current list with the old list, determine if something changed
			new_order.ForEach (addin => {
				if (addin != current_order.ElementAt (i))
					changed = true;
				i++;
			});
			
			// the problem with using this workaround (described above) is that sometimes this
			// even will fire because of ButtonReleased and there may not even be any rows
			// that were reordered.  This is why we have to check the order of the lists and
			// determine if anything was changed.  If no change, bail.
			if (!changed)
				return;
			
			current_order = new_order;

			if (AddinOrderChanged != null)
				AddinOrderChanged (this, new AddinOrderChangedArgs (new_order));
		}
		
		private List<AddinTreeNode> GetAddinOrder () 
		{
			List<AddinTreeNode> addinList = new List<AddinTreeNode> ();
			TreeIter iter;
			
			if (!Model.GetIterFirst (out iter))
				return addinList;
			
			do {
				addinList.Add (Model.GetValue (iter, (int)Column.AddinNode) as AddinTreeNode);
			} while (Model.IterNext (ref iter));
			
			return addinList;
		}
		
		void IconFunction (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			string icon;
			AddinTreeNode node;
			CellRendererPixbuf renderer;

			renderer = cell as CellRendererPixbuf;
			node = (Model as ListStore).GetValue (iter, (int)Column.AddinNode) as AddinTreeNode;
			icon = node.Icon;
			renderer.Pixbuf = DockServices.Drawing.LoadIcon (icon, IconSize);
		}
		
		#region Functions to simplify dealing with an addin
		
		public void Add (AddinTreeNode node)
		{
			store.AppendValues (node.Name, node);
			current_order = GetAddinOrder ();
		}
		
		public void Clear ()
		{
			store.Clear ();
			current_order = GetAddinOrder ();
		}
		
		public void Remove (AddinTreeNode node)
		{
			TreeIter iter;
			store.GetIterFirst (out iter);
			
			do {
				if ((AddinTreeNode) Model.GetValue (iter, (int)Column.AddinNode) == node) {
					store.Remove (ref iter);
					// no need to keep on going...
					return;
				}
			} while (store.IterNext (ref iter));
			current_order = GetAddinOrder ();
		}
		
		public AddinTreeNode SelectedAddin {
			get {
				TreeIter selectedIter;
				
				if (Selection.GetSelected (out selectedIter))
					return (AddinTreeNode) Model.GetValue (selectedIter, (int)Column.AddinNode);
				
				return null;
			}
		}
		
		#endregion
	}
}