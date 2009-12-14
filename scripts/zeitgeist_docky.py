#!/usr/bin/env python

import gobject
import time
import glib
import dbus
import dbus.glib
import sys
import urllib
import os

try:
	from docky.docky import DockyItem, DockySink
	from zeitgeist.client import ZeitgeistClient
	from zeitgeist.datamodel import Event, Subject, Interpretation, Manifestation, StorageState
except ImportError, e:
	exit()

try:
	CLIENT = ZeitgeistClient()
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
						if counter < 7 and exists(subject.uri):
							uris.append(subject)
							counter+=1
						elif counter >= 7:
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
			self._zg.find_event_ids_for_templates([event],_handle_find_events, [delta, today], StorageState.Any, 0, 5) 
		else:
			event.set_actor(path)
			self._zg.find_event_ids_for_templates([event],_handle_find_events, [delta, today], StorageState.Any, 0, 5) 

class DockyZGItem(DockyItem):
	def __init__(self, path):
		DockyItem.__init__(self, path)
		self.mostusedprovider = MostUsedProvider()
		self.update_most_used()
	
	def update_most_used(self):
		uri = ""
		if self.iface.GetOwnsUri():
			uri = self.iface.GetUri();
		elif self.iface.GetOwnsDesktopFile():
			uri = self.iface.GetDesktopFile()
		else:
			return
		
		self.mostusedprovider.get_path_most_used (uri, self._handle_get_most_used, self.iface.GetOwnsUri ())

	def _handle_get_most_used(self, uris):
		for subject in uris:
			menu_id = self.iface.AddFileMenuItem(subject.uri, "Most Used Items")
			self.id_map[menu_id] = subject.uri

class DockyZGSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsUri() or item.GetOwnsDesktopFile():
			self.items[pathtoitem] = DockyZGItem(pathtoitem)

if __name__ == "__main__":
	dockysink = DockyZGSink()
	mainloop = gobject.MainLoop(is_running=True)

	while mainloop.is_running():
		print 'running'
		try:
		    mainloop.run()
		except KeyboardInterrupt:
		    dockysink.dispose ()
		    gobject.idle_add(quit, 1)
		print 'done\n\n'
