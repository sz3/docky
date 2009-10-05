//  
// Copyright (C) 2009 Robert Dyer
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;

using Gtk;
using Mono.Unix;

namespace GMail
{
	[System.ComponentModel.Category("File")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class GMailConfiguration : Bin
	{
		TreeStore labelTreeStore = new TreeStore (typeof (string));

		public GMailConfiguration ()
		{			
			Build ();
			
			labelTreeView.Model = labelTreeStore;
			labelTreeView.Selection.Changed += OnLabelSelectionChanged;
			labelTreeView.AppendColumn (Catalog.GetString ("Labels"), new CellRendererText (), "text", 0);
			UpdateLabels ();
			
			timeout.Value = GMailPreferences.RefreshRate;
		}
		
		void UpdateLabels ()
		{
			labelTreeStore.Clear ();
			for (int i = 0; i < GMailPreferences.Labels.Length; i++)
				labelTreeStore.AppendValues (GMailPreferences.Labels [i]);
		}
		
		protected virtual void OnTimeoutFocusOut (object o, Gtk.FocusOutEventArgs args)
		{
			GMailPreferences.RefreshRate = (uint) timeout.ValueAsInt;
		}
		
		protected virtual void OnLabelSelectionChanged (object o, System.EventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			
			if (((TreeSelection)o).GetSelected (out model, out iter))
			{
				string selected = (string) model.GetValue (iter, 0);
				int index = FindLabelIndex (selected);
				
				label.Text = selected;
				btnMoveUp.Sensitive = index > 0;
				btnRemove.Sensitive = true;
			} else {
				btnMoveUp.Sensitive = false;
				btnMoveDown.Sensitive = false;
				btnRemove.Sensitive = false;
			}
		}
		
		protected virtual void OnAddClicked (object sender, System.EventArgs e)
		{
			if (label.Text.Trim ().Length == 0)
				return;
			if (FindLabelIndex (label.Text) != -1)
				return;
			
			string[] labels = new string [GMailPreferences.Labels.Length + 1];
			Array.Copy (GMailPreferences.Labels, 0, labels, 0, GMailPreferences.Labels.Length);
			labels [GMailPreferences.Labels.Length] = label.Text.Trim ();
			
			GMailPreferences.Labels = labels;
			UpdateLabels ();
		}

		int FindLabelIndex (string label)
		{
			for (int i = 0; i < GMailPreferences.Labels.Length; i++)
				if (GMailPreferences.Labels [i].Equals (label))
					return i;
			
			return -1;
		}

		protected virtual void OnRemoveClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			TreeModel model;
			
			if (labelTreeView.Selection.GetSelected (out model, out iter))
			{
				string removedLabel = (string) model.GetValue (iter, 0);
				int index = FindLabelIndex (removedLabel);
				
				string[] labels = new string [GMailPreferences.Labels.Length - 1];
				Array.Copy (GMailPreferences.Labels, 0, labels, 0, index);
				Array.Copy (GMailPreferences.Labels, index + 1, labels, index, GMailPreferences.Labels.Length - index - 1);
				
				GMailPreferences.Labels = labels;
				
				UpdateLabels ();
			}
		}

		protected virtual void OnMoveDownClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			TreeModel model;
			
			if (labelTreeView.Selection.GetSelected (out model, out iter))
			{
				TreePath[] paths = labelTreeView.Selection.GetSelectedRows ();
				int index = FindLabelIndex ((string) model.GetValue (iter, 0));
				
				string[] labels = GMailPreferences.Labels;
				string temp = labels [index];
				labels [index] = labels [index + 1];
				labels [index + 1] = temp;
				
				GMailPreferences.Labels = labels;
				UpdateLabels ();
				paths [0].Next ();
				labelTreeView.Selection.SelectPath (paths [0]);
				labelTreeView.ScrollToCell (paths [0], null, false, 0, 0);
			}
		}

		protected virtual void OnMoveUpClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			TreeModel model;
			
			if (labelTreeView.Selection.GetSelected (out model, out iter))
			{
				TreePath[] paths = labelTreeView.Selection.GetSelectedRows ();
				int index = FindLabelIndex ((string) model.GetValue (iter, 0));
				
				string[] labels = GMailPreferences.Labels;
				string temp = labels [index];
				labels [index] = labels [index - 1];
				labels [index - 1] = temp;
				
				GMailPreferences.Labels = labels;
				UpdateLabels ();
				paths [0].Prev ();
				labelTreeView.Selection.SelectPath (paths [0]);
				labelTreeView.ScrollToCell (paths [0], null, false, 0, 0);
			}
		}
	}
}
