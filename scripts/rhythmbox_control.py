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
	import gtk
	from docky.docky import DockyItem, DockySink
	from docky.docky import DOCKY_DATADIR
	from signal import signal, SIGTERM
	from sys import exit
except ImportError, e:
	exit()

rhythmboxbus = "org.gnome.Rhythmbox"
playerpath = "/org/gnome/Rhythmbox/Player"
playeriface = "org.gnome.Rhythmbox.Player"

shellpath = "/org/gnome/Rhythmbox/Shell"
shelliface = "org.gnome.Rhythmbox.Shell"

enable_art_icon = True;
enable_badge_text = True;

album_art_tmpfile = "/tmp/docky_%s_rhythmbox_helper.png" % os.getenv('USERNAME')

# 0 - none, 1- jewel, 2 - vinyl)
overlay = 2

class DockyRhythmboxItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)

		self.player = None
		self.shell = None

		self.elapsed_secs = 0
		self.duration_secs = 0
		self.songinfo = None
		self.cover_basename = ""
		self.current_arturl = ""

		self.bus.add_signal_receiver(self.name_owner_changed_cb,
                                             dbus_interface='org.freedesktop.DBus',
                                             signal_name='NameOwnerChanged')
                                             
		obj = self.bus.get_object ("org.freedesktop.DBus", "/org/freedesktop/DBus")
		self.bus_interface = dbus.Interface(obj, "org.freedesktop.DBus")
		
		self.bus_interface.ListNames (reply_handler=self.list_names_handler, error_handler=self.list_names_error_handler)
	
	def list_names_handler(self, names):
		if rhythmboxbus in names:
			self.init_rhythmbox_objects()
			self.set_menu_buttons()
			self.update_text()
			self.update_badge()
			self.update_icon()
	
			
	def list_names_error_handler(self, error):
		print "error getting bus names - %s" % str(error)
	
	
	def name_owner_changed_cb(self, name, old_owner, new_owner):
		if name == rhythmboxbus:
			if new_owner:
				self.init_rhythmbox_objects()
			else:
				self.player = None
				self.shell = None
			self.set_menu_buttons()
			self.update_text()
			self.update_badge()
			self.update_icon()
	
	
	def init_rhythmbox_objects(self):
		obj = self.bus.get_object(rhythmboxbus, playerpath)
		self.player = dbus.Interface(obj, playeriface)

		obj = self.bus.get_object(rhythmboxbus, shellpath)
		self.shell = dbus.Interface(obj, shelliface)

		self.bus.add_signal_receiver(self.signal_playingChanged, "playingChanged",  playeriface, rhythmboxbus, playerpath)
		self.bus.add_signal_receiver(self.signal_elapsedChanged, "elapsedChanged",  playeriface, rhythmboxbus, playerpath)
		self.bus.add_signal_receiver(self.signal_playingUriChanged, "playingUriChanged",  playeriface, rhythmboxbus, playerpath)

		if self.player and self.shell:
			self.update_songinfo(self.player.getPlayingUri())

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
		if self.rhythmbox_is_playing():
			self.add_menu_item("Pause", "media-playback-pause")
		else:
			self.add_menu_item("Play", "media-playback-start")
		self.add_menu_item("Next", "media-skip-forward")
	
	def signal_playingChanged(self, state):
		self.set_menu_buttons()
		self.update_text()
		self.update_icon()

	def signal_elapsedChanged(self, value):
		self.elapsed_secs = value
		self.update_badge()

	def signal_playingUriChanged(self, newuri):
		self.update_songinfo(newuri)
		self.update_text()

	def update_songinfo(self, uri):
		if self.shell:
			try:
				song = dict(self.shell.getSongProperties(uri))
				self.duration_secs = song.get("duration")
				if self.duration_secs > 0:
					self.songinfo = '%s - %s (%i:%02i)' % (song.get("artist", "Unknown"), song.get("title", "Unknown"), song.get("duration") / 60, song.get("duration") % 60)
				else:
					self.songinfo = '%s - %s' % (song.get("artist", "Unknown"), song.get("title", "Unknown"))
				self.cover_basename = "%s - %s" % (song.get("artist"), song.get("album"))
			except dbus.DBusException, e:
				self.duration_secs = 0
				self.songinfo = None
				self.cover_basename = ""
			return
		self.duration_secs = 0
		self.songinfo = None

	def update_icon(self):
		if not self.player:
			self.current_arturl = ""
			self.iface.ResetIcon()
			return False
			
		if not enable_art_icon:
			return True
		
		if self.rhythmbox_is_playing():
			arturl = self.get_album_art_path()
			if not self.current_arturl == arturl:
				# Add overlay to cover
				if os.path.isfile(arturl):
					self.current_arturl = arturl
					self.iface.SetIcon(self.get_album_art_overlay_path(arturl))
				else:
					self.current_arturl = ""
					self.iface.ResetIcon()
		else:
			self.current_arturl = ""
			self.iface.ResetIcon()
		return True
		
	def get_album_art_path(self):
		if not self.player or not self.shell:
			return ""

		arturl = ""
		playinguri = self.player.getPlayingUri()

		#1. Look in song folder
		#TODO need to replace some things, this is very weird
		coverdir = os.path.dirname(playinguri).replace("file://", "").replace("%20", " ")
		covernames = ["cover.jpg", "cover.png", "album.jpg", "album.png", "albumart.jpg", 
			"albumart.png", ".folder.jpg", ".folder.png", "folder.jpg", "folder.png"]
		for covername in covernames:
			arturl = os.path.join(coverdir, covername)
			if os.path.isfile(arturl):
				return arturl

		#2. Check rhythmbox dbus song properties
		arturl = str(self.shell.getSongProperties(playinguri).get("rb:coverArt-uri", ""))
		if not arturl == "":
			return arturl

		#3. Look for cached cover
		arturl = os.path.expanduser("~/.cache/rhythmbox/covers/%s.jpg" % self.cover_basename)

		return arturl
	
	def get_album_art_overlay_path(self, picfile):
		if overlay == 0:
			return picfile

		try:
			pb = gtk.gdk.pixbuf_new_from_file(picfile)
		except Exception, e:
			print e
			return picfile
		
		pb_result = gtk.gdk.Pixbuf(gtk.gdk.COLORSPACE_RGB, True, 8, 250, 250)
		pb_result.fill(0x00000000)

		if overlay == 1:
			overlayfile = os.path.join(DOCKY_DATADIR, "albumoverlay_jewel.png")
			pb.composite(pb_result, 30, 21, 200, 200, 30, 21, 200.0/pb.get_width(), 200.0/pb.get_height(), gtk.gdk.INTERP_BILINEAR, 255)
		elif overlay == 2:
			overlayfile = os.path.join(DOCKY_DATADIR, "albumoverlay_vinyl.png")
			pb.composite(pb_result, 3, 26, 190, 190, 3, 26, 190.0/pb.get_width(), 190.0/pb.get_height(), gtk.gdk.INTERP_BILINEAR, 255)
		else:
			return picfile

		pb_overlay = gtk.gdk.pixbuf_new_from_file_at_size(overlayfile, 250, 250)
		pb_overlay.composite(pb_result, 0, 0, 250, 250, 0, 0, 1, 1, gtk.gdk.INTERP_BILINEAR, 255)
		pb_result.save(album_art_tmpfile, "png", {})
		
		return album_art_tmpfile
		
	def update_text(self):
		if not self.shell or not self.player:
			self.iface.ResetText()

		if self.rhythmbox_is_playing() and self.songinfo:
			self.iface.SetText(self.songinfo)
		else:
			self.iface.ResetText()

	def update_badge(self):
		if not self.player:
			self.iface.ResetBadgeText()
		
		if not enable_badge_text:
			return True
		
		if self.rhythmbox_is_playing():
			#if song length is 0 then counting up instead of down
			if self.duration_secs > 0:
				position = self.duration_secs - self.elapsed_secs
			else:
				position = self.elapsed_secs
			string = '%i:%02i' % (position / 60, position % 60)
			self.iface.SetBadgeText(string)
		else:
			self.iface.ResetBadgeText()
	
	def menu_pressed(self, menu_id):
		if self.id_map[menu_id] == "Play":
			self.rhythmbox_playPause()
		elif self.id_map[menu_id] == "Pause":
			self.rhythmbox_playPause()
		elif self.id_map[menu_id] == "Next":
			self.rhythmbox_next()
		elif self.id_map[menu_id] == "Previous":
			self.rhythmbox_prev()
		
	def add_menu_item(self, name, icon):
		menu_id = self.iface.AddMenuItem(name, icon, "")
		self.id_map[menu_id] = name
		
	def rhythmbox_playPause(self):
		if self.player:
			self.player.playPause(True)
		
	def rhythmbox_next(self):
		if self.player:
			self.player.next()
		
	def rhythmbox_prev(self):
		if self.player:
			self.player.previous()
	
	def rhythmbox_is_playing(self):
		if self.player:
			try:
				return self.player.getPlaying() == 1
			except dbus.DBusException, e:
				return False		
		return False
		
class DockyRhythmboxSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsDesktopFile() and item.GetDesktopFile().endswith ("rhythmbox.desktop"):
			self.items[pathtoitem] = DockyRhythmboxItem(pathtoitem)

dockysink = DockyRhythmboxSink()

def cleanup ():
	dockysink.dispose ()

if __name__ == "__main__":
	mainloop = gobject.MainLoop(is_running=True)

	atexit.register (cleanup)
	
	signal(SIGTERM, lambda signum, stack_frame: exit(1))

	while mainloop.is_running():
		mainloop.run()
