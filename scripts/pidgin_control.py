#!/usr/bin/env python

import atexit
import gobject
import dbus
import dbus.glib
import glib
import sys
import os

try:
	from docky.docky import DockyItem, DockySink
	from signal import signal, SIGTERM
	from sys import exit
except ImportError, e:
	exit()

pidginbus = "im.pidgin.purple.PurpleService"
pidginpath = "/im/pidgin/purple/PurpleObject"
pidginitem = "im.pidgin.purple.PurpleInterface"

class PidginSink():
	def __init__(self):
		bus = dbus.SessionBus()
		obj = bus.get_object (pidginbus, pidginpath)
		self.iface = dbus.Interface (obj, pidginitem)
		
	def IsConnected(self):
		status = self.iface.PurpleSavedstatusGetCurrent()	
		return not self.iface.PurpleSavedstatusGetType(status) == 1

	def IsAway(self):
		status = self.iface.PurpleSavedstatusGetCurrent()	
		return not self.iface.PurpleSavedstatusGetType(status) == 5
		
	def Available(self):
		new_status = self.iface.PurpleSavedstatusNew("", 2)
		self.iface.PurpleSavedstatusActivate(new_status)
	
	def Disconnect(self):
		new_status = self.iface.PurpleSavedstatusNew("", 1)
		self.iface.PurpleSavedstatusActivate(new_status)	
	
	def Away(self):
		new_status = self.iface.PurpleSavedstatusNew("", 5)
		self.iface.PurpleSavedstatusActivate(new_status)
		
class DockyPidginItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)
		self.pidgin = PidginSink()
		dbus.SessionBus ().add_signal_receiver(self.status_changed, "AccountStatusChanged", pidginitem, pidginbus, pidginpath)
		self.set_menu_items()
	
	def status_changed(self, a, b, c):
		self.set_menu_items()
	
	def set_menu_items(self):
		for k, v in self.id_map.iteritems():
			try:
				self.iface.RemoveItem(k)
			except:
				break;
	
		if self.pidgin.IsConnected():
			if self.pidgin.IsAway():
				self.add_menu_item ("Set Away", "/usr/share/pixmaps/pidgin/status/16/away.png", "", "Away")
			else:
				self.add_menu_item ("Set Available", "/usr/share/pixmaps/pidgin/status/16/available.png", "actions", "Connect")
			self.add_menu_item ("Disconnect", "/usr/share/pixmaps/pidgin/status/16/offline.png", "", "Disconnect")
		else:
			self.add_menu_item ("Connect", "/usr/share/pixmaps/pidgin/status/16/available.png", "", "Connect")
		
	
	def menu_pressed(self, menu_id):
		menu_id = self.id_map[menu_id]
		
		if menu_id == "Connect":
			self.pidgin.Available()
		elif menu_id == "Disconnect":
			self.pidgin.Disconnect()
		elif menu_id == "Away":
			self.pidgin.Away()
				
	def add_menu_item(self, name, icon, group, ident):
		menu_id = self.iface.AddMenuItem(name, icon, group)
		self.id_map[menu_id] = ident
			
class DockyPidginSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsDesktopFile() and item.GetDesktopFile().endswith ("pidgin.desktop"):
			self.items[pathtoitem] = DockyPidginItem(pathtoitem)

dockysink = DockyPidginSink()

def cleanup ():
	dockysink.dispose ()

if __name__ == "__main__":
	mainloop = gobject.MainLoop(is_running=True)

	atexit.register (cleanup)
	signal(SIGTERM, lambda signum, stack_frame: exit(1))

	mainloop.run()
