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
	print e
	exit()

lifereabus = "org.gnome.feed.Reader"
readerpath = "/org/gnome/feed/Reader"
readeriface = "org.gnome.feed.Reader"
	
class DockyLifereaItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)
		self.timer = 0
		self.reader = None
				
		self.bus.add_signal_receiver(self.name_owner_changed_cb,
                                             dbus_interface='org.freedesktop.DBus',
                                             signal_name='NameOwnerChanged')
                                             
		obj = self.bus.get_object ("org.freedesktop.DBus", "/org/freedesktop/DBus")
		self.bus_interface = dbus.Interface(obj, "org.freedesktop.DBus")
		
		self.bus_interface.ListNames (reply_handler=self.list_names_handler, error_handler=self.list_names_error_handler)

	def list_names_handler(self, names):
		if lifereabus in names:
			self.init_liferea_objects()
			self.update_badge()

	def list_names_error_handler(self, error):
		print "error getting bus names - %s" % str(error)
	
	def name_owner_changed_cb(self, name, old_owner, new_owner):
		if name == lifereabus:
			if new_owner:
				self.init_liferea_objects()
			else:
				self.reader = None
				if self.timer > 0:
					gobject.source_remove (self.timer)
					self.timer = 0
				self.update_badge()
	
	def init_liferea_objects(self):
		obj = self.bus.get_object(lifereabus, readerpath)
		self.reader = dbus.Interface(obj, readeriface)
		
		if not self.timer > 0:
			self.timer = gobject.timeout_add (10000, self.update_badge)

	def update_badge(self):
		if not self.reader:
			self.iface.ResetBadgeText()
			return False
		
		items_unread = self.reader.GetUnreadItems()
		#items_new = self.reader.GetNewItems()
		if items_unread > 0:
			self.iface.SetBadgeText("%s" % items_unread)
		else:
			self.iface.ResetBadgeText()
		return True

class DockyLifereaSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsDesktopFile() and item.GetDesktopFile().endswith ("liferea.desktop"):
			self.items[pathtoitem] = DockyLifereaItem(pathtoitem)

dockysink = DockyLifereaSink()

def cleanup ():
	dockysink.dispose ()

if __name__ == "__main__":
	mainloop = gobject.MainLoop(is_running=True)

	atexit.register (cleanup)
	signal(SIGTERM, lambda signum, stack_frame: exit(1))

	mainloop.run()
