#!/usr/bin/env python

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

enable_art_icon = False;

class DockyBansheeItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)

		self.init_banshee_objects()
		self.set_menu_buttons()
		self.update_icon()
		
		self.timer = gobject.timeout_add (1000, self.update_badge)
		
	def init_banshee_objects(self):
		bus = dbus.SessionBus()
		obj = bus.get_object(bansheebus, playerpath)
		self.player = dbus.Interface(obj, playeriface)
		
		obj = bus.get_object(bansheebus, controlpath)
		self.control = dbus.Interface(obj, controliface)
		
		bus.add_signal_receiver(self.event_changed, "EventChanged",  playeriface, bansheebus, playerpath)

	def clear_menu_buttons(self):
		for k, v in self.id_map.iteritems():
			try:
				self.iface.RemoveItem(k)
			except:
				break;	
	
	def set_menu_buttons(self):
		self.clear_menu_buttons()
		self.add_menu_item("Previous", "media-skip-backward")
		
		if self.banshee_is_playing():
			self.add_menu_item("Pause", "media-playback-pause")
		else:
			self.add_menu_item("Play", "media-playback-start")
			
		self.add_menu_item("Next", "media-skip-forward")
	
	def event_changed(self, state, st, value):
		if (state == "statechange"):
			self.set_menu_buttons()
		elif (state == "trackinfoupdated"):
			self.set_menu_buttons()
			self.update_icon()
			
	def update_icon(self):
		if enable_art_icon:
			arturl = self.get_album_art_path()
			if os.path.exists(arturl):
				self.iface.SetIcon(arturl)
			else:
				self.iface.ResetIcon()
	
	def update_badge(self):
		if self.banshee_is_playing():
			position = self.player.GetPosition() / 1000
			string = '%i:%02i' % (position / 60, position % 60)

			self.iface.SetBadgeText(string)
		else:
			self.iface.ResetBadgeText()
		return True;
	
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
		self.player.Play()
		
	def banshee_pause(self):
		self.player.Pause()
	
	def banshee_next(self):
		self.control.Next(False)
		
	def banshee_prev(self):
		self.control.Previous(False)
		
	def banshee_is_playing(self):
		return self.player.GetCurrentState() == "playing"
	
	def get_album_art_path(self):
		user = os.getenv("USER")
		arturl = '/home/'
		arturl += user
		arturl += '/.cache/album-art/'
		arturl += self.player.GetCurrentTrack().get("artwork-id")
		arturl += '.jpg'
		return arturl
	
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
