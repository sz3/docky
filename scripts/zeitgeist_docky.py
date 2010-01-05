#!/usr/bin/env python

#  
#  Copyright (C) 2009 Jason Smith, Seif Lofty
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
import time
import glib
import dbus
import gtk
import dbus.glib
import sys
import urllib
import os

try:
	from docky.docky import DockyItem, DockySink
	from zeitgeist.client import ZeitgeistClient
	from zeitgeist.datamodel import Event, Subject, Interpretation, Manifestation, StorageState
	from signal import signal, SIGTERM
	from sys import exit
except ImportError, e:
	exit()

try:
	CLIENT = ZeitgeistClient()
	version = CLIENT.get_version()
	MIN_VERSION = [0, 2, 99]
	for i in xrange(3):
		if version[i] < MIN_VERSION[i]:
			print "PLEASE USE ZEITGEIST 0.3.0 or above"
			exit()
		

except RuntimeError, e:
	print "Unable to connect to Zeitgeist, won't send events. Reason: '%s'" %e
	exit()
	
class MostUsedProvider():
	def __init__(self):
		self._zg = CLIENT

	def get_path_most_used(self, path, handler, is_directoy=True):
		today = time.time() * 1000
		delta = (today - 14 * 86400000)

		def exists(uri):
		 	return not uri.startswith("file://") or os.path.exists(urllib.unquote(str(uri[7:])))

		def _handle_find_events(ids):
			self._zg.get_events(ids, _handle_get_events)

		def _handle_get_events(events):
			uris = []
			counter = 0
			for event in events:
				if counter < 5:
					for subject in event.subjects:
						if counter < 5 and exists(subject.uri):
							uris.append(subject)
							counter+=1
						elif counter >= 5:
							break
						else:
							pass
							#print "skipping", subject.uri
				else:
					break
			handler(uris)
		
		event = Event()
		if is_directoy:
			subject = Subject()
			subject.set_origin(path)
			event.set_subjects([subject])
			self._zg.find_event_ids_for_templates([event],_handle_find_events, [delta, today], StorageState.Any, 0, 4) 
		else:
			event.set_actor(path)
			self._zg.find_event_ids_for_templates([event],_handle_find_events, [delta, today], StorageState.Any, 0, 4) 

class DockyZGItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)
		self.mostusedprovider = MostUsedProvider()
		self.update_most_used()
	
	def update_most_used(self):
		self.uri = ""
		if self.iface.GetOwnsUri():
			self.uri = self.iface.GetUri();
		elif self.iface.GetOwnsDesktopFile():
			self.uri = self.iface.GetDesktopFile()
		else:
			return
		self.mostusedprovider.get_path_most_used (self.uri, self._handle_get_most_used, self.iface.GetOwnsUri ())

	def _handle_get_most_used(self, uris):
		for subject in uris:
			menu_id = self.iface.AddFileMenuItem(subject.uri,"Most Used Items")
			self.id_map[menu_id] = subject.uri

class DockyZGSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsUri() or item.GetOwnsDesktopFile():
			self.items[pathtoitem] = DockyZGItem(pathtoitem)

dockysink = DockyZGSink()

def cleanup ():
	dockysink.dispose ()

if __name__ == "__main__":
	mainloop = gobject.MainLoop(is_running=True)

	atexit.register (cleanup)
	
	signal(SIGTERM, lambda signum, stack_frame: exit(1))

	while mainloop.is_running():
	    mainloop.run()
