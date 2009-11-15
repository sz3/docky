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
using System.Linq;
using System.Collections.Generic;

using GLib;

using Docky.Items;
using Gtk;

namespace RecentDocuments
{
	public class RecentDocumentsItemProvider : AbstractDockItemProvider
	{
		#region AbstractDockItemProvider implementation
		public override string Name {
			get {
				return "Recent Documents";
			}
		}
		
		public override string Icon {
			get {
				return "document-open-recent";
			}
		}
		
		public override void Dispose ()
		{
			docs.Dispose ();
		}
		
		#endregion
		
		RecentDocumentsItem docs;
		
		public RecentDocumentsItemProvider ()
		{
			docs = new RecentDocumentsItem ();
			Items = docs.AsSingle<AbstractDockItem> ();
			Gtk.RecentManager.Default.Changed += delegate {
				if (Gtk.RecentManager.Default.Size == 0) {
					Items = Enumerable.Empty<AbstractDockItem> ();
				} else {
					Items = docs.AsSingle<AbstractDockItem> ();
				}
			};
		}
	}
}
