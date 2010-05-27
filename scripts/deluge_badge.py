#!/usr/bin/env python

#  
#  Copyright (C) 2010 Rico Tzschichholz, Robert Dyer
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
import math

try:
	from deluge.ui.client import sclient
	from docky.dockmanager import DockManagerItem, DockManagerSink
	from signal import signal, SIGTERM
	from sys import exit
except ImportError, e:
	print e
	exit()

ShowUploadRate = False


def bytes2ratestr(bytes):
	for factor, suffix in (
		(1024 ** 5, 'P'),
		(1024 ** 4, 'T'), 
		(1024 ** 3, 'G'), 
		(1024 ** 2, 'M'), 
		(1024 ** 1, 'K'),
		(1024 ** 0, 'B')):
		if bytes >= factor:
			break
	amount = int(bytes / factor)
	return str(amount) + suffix
	

class DelugeItem(DockManagerItem):
	def __init__(self, sink, path):
		DockManagerItem.__init__(self, sink, path)
		
		self.timer = 0
		
		sclient.set_core_uri()
	
		if not self.timer > 0:
			self.timer = gobject.timeout_add (2000, self.update_badge)

	def update_badge(self):
		rate = 0
		try:
			if ShowUploadRate:
				rate = round(sclient.get_upload_rate())
			else:	
				rate = round(sclient.get_download_rate())
	
			if rate > 0:
				self.set_badge("%s" % bytes2ratestr(rate))
			else:
				self.reset_badge()
			return True
		except Exception, e:
			print e
			self.reset_badge()
			return False
	

class DelugeSink(DockManagerSink):
	def item_path_found(self, pathtoitem, item):
		if item.Get("org.freedesktop.DockItem", "DesktopFile", dbus_interface="org.freedesktop.DBus.Properties").endswith ("deluge.desktop"):
			self.items[pathtoitem] = DelugeItem(self, pathtoitem)

delugesink = DelugeSink()

def cleanup ():
	delugesink.dispose ()

if __name__ == "__main__":
	mainloop = gobject.MainLoop(is_running=True)

	atexit.register (cleanup)
	signal(SIGTERM, lambda signum, stack_frame: exit(1))

	mainloop.run()
