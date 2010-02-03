#!/usr/bin/env python

#  
#  Copyright (C) 2009-2010 Jason Smith, Rico Tzschichholz
#                2010 Lukasz Piepiora
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
		self.pidgin = None
		
		self.bus.add_signal_receiver(self.name_owner_changed_cb,
				dbus_interface='org.freedesktop.DBus',
				signal_name='NameOwnerChanged')
		
		obj = self.bus.get_object ("org.freedesktop.DBus", "/org/freedesktop/DBus")
		self.bus_interface = dbus.Interface(obj, "org.freedesktop.DBus")
		
		self.bus_interface.ListNames (reply_handler=self.list_names_handler, error_handler=self.list_names_error_handler)
		
		self.bus.add_signal_receiver(self.status_changed, "AccountStatusChanged", pidginitem, pidginbus, pidginpath)
		self.bus.add_signal_receiver(self.new_chat_or_message, "ReceivedImMsg", pidginitem, pidginbus, pidginpath)
		self.bus.add_signal_receiver(self.new_chat_or_message, "ReceivedChatMsg", pidginitem, pidginbus, pidginpath)

	def list_names_handler(self, names):
		if pidginbus in names:
			self.pidgin = PidginSink()
			self.set_menu_buttons()
			self.update_badge()

	def list_names_error_handler(self, error):
		print "error getting bus names - %s" % str(error)
	
	def name_owner_changed_cb(self, name, old_owner, new_owner):
		if name == pidginbus:
			if new_owner:
				self.pidgin = PidginSink()
			else:
				self.pidgin = None
			self.set_menu_buttons()
			self.update_badge()
	
	def init_pidgin_objects(self):
		self.pidgin = PidginSink()

	def status_changed(self, a, b, c):
		self.set_menu_buttons()
		self.update_badge()
	
	def new_chat_or_message(self, account, sender, message, conv, flags):
		self.update_badge()
	
	def clear_menu_buttons(self):
		for k, v in self.id_map.iteritems():
			try:
				self.iface.RemoveItem(k)
			except:
				break;	
	
	def set_menu_buttons(self):
		self.clear_menu_buttons()
				
		if not self.pidgin or not self.iface:
			return

		if self.pidgin.IsConnected():
			if self.pidgin.IsAway():
				self.add_menu_item ("Set Away", "/usr/share/pixmaps/pidgin/status/16/away.png", "", "Away")
			else:
				self.add_menu_item ("Set Available", "/usr/share/pixmaps/pidgin/status/16/available.png", "", "Connect")
			self.add_menu_item ("Disconnect", "/usr/share/pixmaps/pidgin/status/16/offline.png", "", "Disconnect")
		else:
			self.add_menu_item ("Connect", "/usr/share/pixmaps/pidgin/status/16/available.png", "", "Connect")
		
	def update_badge(self):
		if not self.pidgin:
			self.iface.ResetBadgeText()
			return False
		
		convs = self.pidgin.iface.PurpleGetConversations()
		count = 0
		for conv in convs:
			count = count + self.pidgin.iface.PurpleConversationGetData(conv, "unseen-count")
		if count:
			self.iface.SetBadgeText("%s" % count)
		else:
			self.iface.ResetBadgeText()
		return True
	
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
