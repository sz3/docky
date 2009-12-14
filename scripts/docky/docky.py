#!/usr/bin/env python

import gobject
import time
import glib
import dbus
import dbus.glib
import sys
import urllib
import os

dockypath  = '/org/gnome/Docky'
dockybus   = 'org.gnome.Docky'
dockyiface = 'org.gnome.Docky'
itemiface  = 'org.gnome.Docky.Item'

class DockyItem():
	def __init__(self, path):
		self.path = path
		self.bus = dbus.SessionBus()
		self.id_map = {}
		
		obj = self.bus.get_object(dockybus, self.path)
		self.iface = dbus.Interface(obj, itemiface)
	
		self.bus.add_signal_receiver(self.menu_pressed_signal, "MenuItemActivated", itemiface, dockybus, self.path)
		
		self.timer = glib.timeout_add(2 * 60 * 1000, self.handle_timeout)
	
	def dispose(self):
		for k, v in self.id_map.iteritems():
			try:
				self.iface.RemoveItem(k)
			except:
				break;
		glib.source_remove(self.timer)
	
	def handle_timeout(self):
		for k, v in self.id_map.iteritems():
			self.iface.ConfirmItem(k)
		return True

class DockySink():
	def __init__(self):
		self.bus = dbus.SessionBus()
		self.items = {}
		self.disposed = False;

		obj = self.bus.get_object(dockybus, dockypath)
		self._iface = dbus.Interface(obj, dockyiface)

		paths = self._iface.DockItemPaths()
		
		self.bus.add_signal_receiver(self.item_added,   "ItemAdded",    dockyiface, dockybus, dockypath)
		self.bus.add_signal_receiver(self.item_removed, "ItemRemoved",  dockyiface, dockybus, dockypath)
		self.bus.add_signal_receiver(self.shut_down,    "ShuttingDown", dockyiface, dockybus, dockypath)
		
		for pathtoitem in paths:
			obj = self.bus.get_object(dockybus, pathtoitem)
			item = dbus.Interface(obj, itemiface)
			self.item_path_found(pathtoitem, item)
	
	def item_added(self, path):
		if self.disposed:
			return;
		obj = self.bus.get_object(dockybus, path)
		item = dbus.Interface(obj, itemiface)
		self.item_path_found(path, item)

	def item_removed(self, path):
		if self.disposed:
			return;
		if path in self.items:
			self.items[path].dispose()
			del self.items[path]
	
	def shut_down(self):
		self.dispose()
		gobject.idle_add(quit, 1)
	
	def dispose(self):
		self.disposed = True;
		for path, item in self.items.iteritems():
			item.dispose()

