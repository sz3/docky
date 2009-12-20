#!/usr/bin/env python

import atexit
import gobject
import glib
import sys
import os

try:
	from docky.docky import DockyItem, DockySink
	from signal import signal, SIGTERM
	from sys import exit
except ImportError, e:
	exit()

class DockyBansheeItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)
		
		self.add_menu_item("Previous", "media-skip-backward")
		self.add_menu_item("Play/Pause", "media-playback-start")
		self.add_menu_item("Next", "media-skip-forward")

	def menu_pressed(self, menu_id):
		if self.id_map[menu_id] == "Toggle Playing":
			os.system ("banshee --toggle-playing")
		elif self.id_map[menu_id] == "Next":
			os.system ("banshee --next")
		elif self.id_map[menu_id] == "Previous":
			os.system ("banshee --previous")
		
	def add_menu_item(self, name, icon):
		menu_id = self.iface.AddMenuItem(name, icon, "actions")
		self.id_map[menu_id] = name
			
class DockyBansheeSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsDesktopFile() and item.GetDesktopFile().endswith ("banshee-1.desktop"):
			self.items[pathtoitem] = DockyBansheeItem(pathtoitem)

dockysink = DockyBansheeSink()

def cleanup ():
	dockysink.dispose ()

if __name__ == "__main__":
	mainloop = gobject.MainLoop(is_running=True)

	atexit.register (cleanup)
	signal(SIGTERM, lambda signum, stack_frame: exit(1))

	mainloop.run()
