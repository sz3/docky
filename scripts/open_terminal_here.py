#!/usr/bin/env python

#  
#  Copyright (C) 2009 Jason Smith
# 
#  This program is free software: you can redistribute it and/or modify
#  it under the terms of the GNU General Public License as published by
#  the Free Software Foundation, either version 3 of the License, or
#  (at your option) any later version.
# 
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU General Public License for more details.
# 
#  You should have received a copy of the GNU General Public License
#  along with this program.  If not, see <http://www.gnu.org/licenses/>.
#

import atexit
import gobject
import glib
import sys
import urllib
import os

try:
	from docky.docky import DockyItem, DockySink
	from signal import signal, SIGTERM
	from sys import exit
except ImportError, e:
	exit()

class DockyTerminalItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)
		self.path = urllib.unquote(str(self.iface.GetUri ()[7:]))
		if not os.path.isdir (self.path):
			self.path = os.path.dirname (self.path)
		
		self.add_menu_item("Open Terminal Here", "terminal")

	def menu_pressed(self, menu_id):
		if self.id_map[menu_id] == "Open Terminal Here":
			os.system ("gnome-terminal --working-directory=\"" + self.path + "\"")
		
	def add_menu_item(self, name, icon):
		menu_id = self.iface.AddMenuItem(name, icon, "actions")
		self.id_map[menu_id] = name
			
class DockyTerminalSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsUri() and item.GetUri().startswith ("file://"):
			self.items[pathtoitem] = DockyTerminalItem(pathtoitem)

dockysink = DockyTerminalSink()

def cleanup ():
	dockysink.dispose ()

if __name__ == "__main__":
	mainloop = gobject.MainLoop(is_running=True)

	atexit.register (cleanup)
	signal(SIGTERM, lambda signum, stack_frame: exit(1))

	mainloop.run()
