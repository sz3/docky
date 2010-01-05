#!/usr/bin/env python

#  
#  Copyright (C) 2009-2010 Jason Smith, Rico Tzschichholz
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
import dbus
import dbus.glib
import sys
import os

try:
	from docky.docky import DockyItem, DockySink
	from signal import signal, SIGTERM
	from sys import exit
except ImportError, e:
	exit()

bansheebus = "org.bansheeproject.Banshee"
playerpath = "/org/bansheeproject/Banshee/PlayerEngine"
playeriface = "org.bansheeproject.Banshee.PlayerEngine"

controlpath = "/org/bansheeproject/Banshee/PlaybackController"
controliface = "org.bansheeproject.Banshee.PlaybackController"

enable_art_icon = True;
enable_badge_text = True;

class DockyBansheeItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)
		self.timer = 0
		self.player = None
		self.control = None
		
		self.duration_secs = 0
		self.songinfo = None
		
		self.bus.add_signal_receiver(self.name_owner_changed_cb,
                                             dbus_interface='org.freedesktop.DBus',
                                             signal_name='NameOwnerChanged')
                                             
		obj = self.bus.get_object ("org.freedesktop.DBus", "/org/freedesktop/DBus")
		self.bus_interface = dbus.Interface(obj, "org.freedesktop.DBus")
		
		self.bus_interface.ListNames (reply_handler=self.list_names_handler, error_handler=self.list_names_error_handler)
	
	def list_names_handler(self, names):
		if bansheebus in names:
			self.init_banshee_objects()
			self.set_menu_buttons()
			self.update_text()			
			self.update_badge()
			self.update_icon()

	def list_names_error_handler(self, error):
		print "error getting bus names - %s" % str(error)
	
	def name_owner_changed_cb(self, name, old_owner, new_owner):
		if name == bansheebus:
			if new_owner:
				self.init_banshee_objects()
			else:
				self.player = None
				self.control = None
				if self.timer > 0:
					gobject.source_remove (self.timer)
					self.timer = 0
				self.set_menu_buttons()
				self.update_text()			
				self.update_badge()
				self.update_icon()
	
	def init_banshee_objects(self):
		obj = self.bus.get_object(bansheebus, playerpath)
		self.player = dbus.Interface(obj, playeriface)
		
		obj = self.bus.get_object(bansheebus, controlpath)
		self.control = dbus.Interface(obj, controliface)
		
		self.bus.add_signal_receiver(self.signal_EventChanged, "EventChanged",  playeriface, bansheebus, playerpath)
		self.bus.add_signal_receiver(self.signal_StateChanged, "StateChanged",  playeriface, bansheebus, playerpath)

		if self.player:
			self.update_songinfo()

		if not self.timer > 0:
			self.timer = gobject.timeout_add (1000, self.update_badge)

	def clear_menu_buttons(self):
		for k, v in self.id_map.iteritems():
			try:
				self.iface.RemoveItem(k)
			except:
				break;	
	
	def set_menu_buttons(self):
		self.clear_menu_buttons()
		
		if not self.player:
			return

		self.add_menu_item("Previous", "media-skip-backward")
		if self.banshee_is_playing():
			self.add_menu_item("Pause", "media-playback-pause")
		else:
			self.add_menu_item("Play", "media-playback-start")
		self.add_menu_item("Next", "media-skip-forward")
	
	def signal_EventChanged(self, event, st, value):
		print "EventChanged: %s" % (event)	
		if (event in ["startofstream", "trackinfoupdated"]):
			self.update_songinfo();
			self.update_text()
			self.update_badge()
			self.update_icon()
		elif (event in ["endofstream"]):
			self.update_icon()
			self.update_text()
		elif (event in ["seek"]):
			self.update_badge()

	def signal_StateChanged(self, state):
		print "StateChanged: %s" % (state)	
		self.set_menu_buttons()
		self.update_text()
			
	def update_songinfo(self):
		if self.player:
			try:
				song = self.player.GetCurrentTrack()
				self.duration_secs = self.player.GetLength() / 1000
				if self.duration_secs > 0:
					self.songinfo = '%s - %s (%i:%02i)' % (song.get("artist", "Unknown"), song.get("name", "Unknown"), self.duration_secs / 60, self.duration_secs % 60)
				else:
					self.songinfo = '%s - %s' % (song.get("artist", "Unknown"), song.get("name", "Unknown"))
			except dbus.DBusException, e:
				self.duration_secs = 0
				self.songinfo = None
			return
		self.duration_secs = 0
		self.songinfo = None
	
	def update_icon(self):
		if not self.player:
			self.iface.ResetIcon()
			return False
			
		if not enable_art_icon:
			return True
		
		if self.banshee_is_playing():
			arturl = self.get_album_art_path()
			if os.path.exists(arturl):
				self.iface.SetIcon(arturl)
			else:
				self.iface.ResetIcon()
		else:
			self.iface.ResetIcon()
		return True

	def get_album_art_path(self):
		artwork_id = self.player.GetCurrentTrack().get("artwork-id")

		if not self.player or not artwork_id:
			return ""

		arturl = os.path.expanduser("~/.cache/album-art/%s.jpg" % artwork_id)
		return arturl

	def update_text(self):
		if not self.player:
			self.iface.ResetText()

		if self.banshee_is_playing() and self.songinfo:
			self.iface.SetText(self.songinfo)
		else:
			self.iface.ResetText()
	
	def update_badge(self):
		if not self.player:
			self.iface.ResetBadgeText()
			return False

		if not enable_badge_text:
			return True
		
		if self.banshee_is_playing():
			#if song length is 0 then counting up instead of down
			if self.duration_secs > 0:
				position = self.duration_secs - self.player.GetPosition() / 1000
			else:
				position = self.player.GetPosition() / 1000
			string = '%i:%02i' % (position / 60, position % 60)
			self.iface.SetBadgeText(string)
		else:
			self.iface.ResetBadgeText()
		return True
	
	def menu_pressed(self, menu_id):
		if self.id_map[menu_id] == "Play":
			self.banshee_play()
		elif self.id_map[menu_id] == "Pause":
			self.banshee_pause()
		elif self.id_map[menu_id] == "Next":
			self.banshee_next()
		elif self.id_map[menu_id] == "Previous":
			self.banshee_prev()
		
	def add_menu_item(self, name, icon):
		menu_id = self.iface.AddMenuItem(name, icon, "")
		self.id_map[menu_id] = name
		
	def banshee_play(self):
		if self.player:
			self.player.Play()
		
	def banshee_pause(self):
		if self.player:
			self.player.Pause()
	
	def banshee_next(self):
		if self.control:
			self.control.Next(False)
		
	def banshee_prev(self):
		if self.control:
			self.control.Previous(False)
		
	def banshee_is_playing(self):
		if self.player:
			try:
				return self.player.GetCurrentState() == "playing"
			except dbus.DBusException, e:
				return False
		return False
	

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

	while mainloop.is_running():
		mainloop.run()
