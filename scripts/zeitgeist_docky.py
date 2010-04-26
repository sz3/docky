#!/usr/bin/env python

#  
#  Copyright (C) 2009 Jason Smith, Seif Lotfy
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
	version = [int(x) for x in CLIENT.get_version()]
	MIN_VERSION = [0, 3, 2, 0]
	if version < MIN_VERSION:
		print "PLEASE USE ZEITGEIST 0.3.2 or above"
		exit()
		

except RuntimeError, e:
	print "Unable to connect to Zeitgeist, won't send events. Reason: '%s'" %e
	exit()
	
class MostUsedProvider():
	def __init__(self):
		self._zg = CLIENT
		self.results = []

	def get_path_most_used(self, path, handler, is_directoy=True):
		today = time.time() * 1000
		delta = (today - 14 * 86400000)

		def exists(uri):
		 	return not uri.startswith("file://") or os.path.exists(urllib.unquote(str(uri[7:])))

		def _handle_find_events(ids):
			self._zg.get_events(ids, _handle_get_events)

		def _handle_get_events(events):
			uris = []
			uris_counter = {}
			for event in events:
				for subject in event.subjects:
					if exists(subject.uri):
						if not subject.uri in uris:
							uris.append(subject.uri)
							uris_counter[subject.uri] = 0
						uris_counter[subject.uri] += 1
						
			counter = []
			for k, v in uris_counter.iteritems():
				counter.append((v, k))
			counter.sort(reverse = True)

			recent =[]
			temp = [i[1] for i in counter]
			for uri in uris:
				if not uri in temp[0:5]:
					recent.append(uri)
			results = []
			results.append(recent[0:5])

			results.append(counter[0:5])
			handler(results)
		
		event = Event()
		if is_directoy:
			subject = Subject()
			subject.set_origin(path)
			event.set_subjects([subject])
			self._zg.find_events_for_templates([event],_handle_get_events, [delta, today], StorageState.Any, 0, 0) 
		else:
			path = "application://" + path.split("/")[-1]
			event.set_actor(path)
			self._zg.find_events_for_templates([event],_handle_get_events, [delta, today], StorageState.Any, 0, 0)  


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

	def _handle_get_most_used(self, results):
		uris = results[0]
		if len(uris) > 0:
			for subject in uris:
				menu_id = self.iface.AddFileMenuItem(subject ,"Other Recently Used Items")
				self.id_map[menu_id] = subject
		uris = results[1]
		if len(uris) > 0:
			for subject in uris:
				menu_id = self.iface.AddFileMenuItem(subject[1], "Most Used Items")
				self.id_map[menu_id] = subject

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
