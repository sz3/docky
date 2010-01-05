#!/usr/bin/env python

#  
#  Copyright (C) 2009 Jason Smith, Robert Dyer
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
		self.bus.add_signal_receiver(self.item_confirmation_needed, "ItemConfirmationNeeded", itemiface, dockybus, self.path)
		
	def menu_pressed_signal(self, menu_id):
		if self.id_map.has_key(menu_id):
			self.menu_pressed(menu_id)
	
	def item_confirmation_needed (self):
		for k,v in self.id_map.iteritems():
			self.iface.ConfirmItem(k)
	
	def dispose(self):
		self.iface.ResetText()
		self.iface.ResetBadgeText()
		self.iface.ResetIcon()
		for k, v in self.id_map.iteritems():
			try:
				self.iface.RemoveItem(k)
			except:
				break;
	
	def menu_pressed(self, menu_id):
		pass

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
		gobject.idle_add(quit, 1)
	
	def dispose(self):
		self.disposed = True;
		for path, item in self.items.iteritems():
			item.dispose()

