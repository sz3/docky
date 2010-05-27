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


dockmanagerbus   = 'org.freedesktop.DockManager'
dockmanagerpath  = '/org/freedesktop/DockManager'
dockmanageriface = 'org.freedesktop.DockManager'

itemiface        = 'org.freedesktop.DockItem'


class DockManagerItem():
	def __init__(self, sink, path):
		self.sink = sink;
		self.path = path
		self.bus = dbus.SessionBus()
		self.id_map = {}
		
		try:
			obj = self.bus.get_object(dockmanagerbus, self.path)
			self.iface = dbus.Interface(obj, itemiface)

			self.bus.add_signal_receiver(self.menu_pressed_signal, "MenuItemActivated", itemiface, dockmanagerbus, self.path)
			if "x-docky-uses-timeout" in self.sink.capabilities:
				self.bus.add_signal_receiver(self.item_confirmation_needed, "ItemConfirmationNeeded", itemiface, dockmanagerbus, self.path)
		except dbus.DBusException, e:
			print "DockManagerItem(): %s" % e
			sys.exit(0)
		
	def add_menu_item(self, name, icon):
		return add_menu_item(self, name, icon, "")
	
	def add_menu_item(self, name, icon, group):
		try:
			menu_id = self.iface.AddMenuItem({"label":name, "icon-name":icon, "container-title":group})
		except dbus.DBusException, e:
			return None
		self.id_map[menu_id] = name
		return menu_id

	def add_menu_item_uri(self, uri, group):
		try:
			menu_id = self.iface.AddMenuItem({"uri":uri, "container-title":group})
		except dbus.DBusException, e:
			return None
		self.id_map[menu_id] = uri
		return menu_id

	def remove_menu_item(self, menu_id):
		if menu_id in self.id_map:
			try:
				self.iface.RemoveMenuItem(menu_id)
			except dbus.DBusException, e:
				return None
			del self.id_map[menu_id]

	def menu_pressed_signal(self, menu_id):
		if self.id_map.has_key(menu_id):
			self.menu_pressed(menu_id)
	
	def menu_pressed(self, menu_id):
		pass
	
	def item_confirmation_needed(self):
		if "x-docky-uses-timeout" in self.sink.capabilities:
			for menu_id, title in self.id_map.iteritems():
				try:
					self.iface.ConfirmItem(menu_id)
				except dbus.DBusException, e:
					pass
	
	def set_tooltip(self, text):
		if ("dock-item-tooltip" in self.sink.capabilities):
			try:
				self.iface.UpdateDockItem({"tooltip":text})
			except dbus.DBusException, e:
				pass
	
	def reset_tooltip(self):
		self.set_tooltip("")
	
	def set_badge(self, text):
		if ("dock-item-badge" in self.sink.capabilities):
			try:
				self.iface.UpdateDockItem({"badge":text})
			except dbus.DBusException, e:
				pass
	
	def reset_badge(self):
		self.set_badge("")
	
	def set_icon(self, icon):
		if ("dock-item-icon-file" in self.sink.capabilities):
			try:
				self.iface.UpdateDockItem({"icon-file":icon})
			except dbus.DBusException, e:
				pass
	
	def reset_icon(self):
		self.set_icon("")
	
	def set_attention(self):
		if ("dock-item-attention" in self.sink.capabilities):
			try:
				self.iface.UpdateDockItem({"attention":True})
			except dbus.DBusException, e:
				pass
	
	def unset_attention(self):
		if ("dock-item-attention" in self.sink.capabilities):
			try:
				self.iface.UpdateDockItem({"attention":False})
			except dbus.DBusException, e:
				pass
	
	def set_waiting(self):
		if ("dock-item-waiting" in self.sink.capabilities):
			try:
				self.iface.UpdateDockItem({"waiting":True})
			except dbus.DBusException, e:
				pass
	
	def unset_waiting(self):
		if ("dock-item-waiting" in self.sink.capabilities):
			try:
				self.iface.UpdateDockItem({"waiting":False})
			except dbus.DBusException, e:
				pass
	
	def dispose(self):
		try:
			self.reset_tooltip()
			self.reset_badge()
			self.reset_icon()
			for menu_id, title in self.id_map.iteritems():
				self.iface.RemoveMenuItem(menu_id)
		except dbus.DBusException, e:
			return


class DockManagerSink():
	def __init__(self):
		self.bus = dbus.SessionBus()
		self.capabilities = []
		self.items = {}

		try:
			obj = self.bus.get_object(dockmanagerbus, dockmanagerpath)
			iface = dbus.Interface(obj, dockmanageriface)

			self.capabilities = iface.GetCapabilities()

			for pathtoitem in iface.GetItems():
				self.item_added(pathtoitem)

			self.bus.add_signal_receiver(self.item_added,   "ItemAdded",   dockmanageriface, dockmanagerbus, dockmanagerpath)
			self.bus.add_signal_receiver(self.item_removed, "ItemRemoved", dockmanageriface, dockmanagerbus, dockmanagerpath)

			self.bus.add_signal_receiver(self.name_owner_changed_cb, dbus_interface='org.freedesktop.DBus', signal_name='NameOwnerChanged')
		except dbus.DBusException, e:
			print "DockManagerSink(): %s" % e
			sys.exit(0)

	def item_path_found(self, path, item):
		pass
	
	def item_added(self, path):
		obj = self.bus.get_object(dockmanagerbus, path)
		item = dbus.Interface(obj, itemiface)
		self.item_path_found(path, item)

	def item_removed(self, path):
		if path in self.items:
			self.items[path].dispose()
			del self.items[path]

	def name_owner_changed_cb(self, name, old_owner, new_owner):
		if name == dockmanagerbus and not new_owner:
			print "DockManagerDBus %s is gone, quitting now..." % name
			self.shut_down()				
	
	def dispose(self):
		self.bus.remove_signal_receiver(self.item_added,   "ItemAdded",   dockmanageriface, dockmanagerbus, dockmanagerpath)
		self.bus.remove_signal_receiver(self.item_removed, "ItemRemoved", dockmanageriface, dockmanagerbus, dockmanagerpath)
		for path in self.items:
			self.item_removed(path)
	
	def shut_down(self):
		gobject.idle_add(quit, 1)

