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
	from zeitgeist.client import ZeitgeistClient
	from zeitgeist.datamodel import Event, Subject, Interpretation, Manifestation, StorageState
except ImportError, e:
	exit()

dockypath  = '/org/gnome/Docky'
dockybus   = 'org.gnome.Docky'
dockyiface = 'org.gnome.Docky'
itemiface  = 'org.gnome.Docky.Item'

try:
	CLIENT = ZeitgeistClient()
except RuntimeError, e:
	print "Unable to connect to Zeitgeist, won't send events. Reason: '%s'" %e
	CLIENT = None

class MostUsedProvider():
	def __init__(self):
		self._zg = CLIENT

	def get_path_most_used( self, path, handler, is_directoy=True):
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
			handler( uris)

		event = Event()
		if is_directoy:
			subject = Subject()
			subject.set_origin(path)
			event.set_subjects([subject])
			self._zg.find_event_ids_for_templates([event],_handle_find_events, [delta, today], StorageState.Any, 0, 5) 
		else:
			event.set_actor(path)
			self._zg.find_event_ids_for_templates([event],_handle_find_events, [delta, today], StorageState.Any, 0, 5) 

class DockyItem():
	def __init__(self, path):
		self.path = path
		self.bus = dbus.SessionBus ()
		self.id_map = {}
		
		obj = self.bus.get_object (dockybus, self.path)
		self.iface = dbus.Interface (obj, itemiface)
	
		self.bus.add_signal_receiver (self.menu_pressed_signal, "MenuItemActivated", itemiface, dockybus, self.path)
	
		self.mostusedprovider = MostUsedProvider ()
		
		self.update_most_used ()
		self.timer = glib.timeout_add (2 * 60 * 1000, self.handle_timeout)
	
	
	def dispose(self):
		for k, v in self.id_map.iteritems():
			try:
				self.iface.RemoveItem (k)
			except:
				break;
		glib.source_remove (self.timer)
	
	def handle_timeout(self):
		for k, v in self.id_map.iteritems():
			self.iface.ConfirmItem (k)
		return True

	def update_most_used(self):
		uri = ""
		if self.iface.GetOwnsUri ():
			uri = self.iface.GetUri ();
		elif self.iface.GetOwnsDesktopFile ():
			uri = self.iface.GetDesktopFile()
		else:
			return
			
		self.mostusedprovider.get_path_most_used (uri, self._handle_get_most_used, self.iface.GetOwnsUri ())

	def _handle_get_most_used(self, uris):
		for subject in uris:
			menu_id = self.iface.AddMenuItem (subject.text, "gtk-file", "Most Used Items")
			self.id_map[menu_id] = subject.uri
		
	def menu_pressed_signal(self, menu_id):
		os.spawnlp(os.P_NOWAIT, "gnome-open", "gnome-open", self.id_map[menu_id])

class DockySink():
	def __init__(self):
		self.bus = dbus.SessionBus ()
		self.items = {}
		self.disposed = False;

		obj = self.bus.get_object (dockybus, dockypath)
		self._iface = dbus.Interface (obj, dockyiface)

		paths = self._iface.DockItemPaths()
		
		self.bus.add_signal_receiver (self.item_added,   "ItemAdded",    dockyiface, dockybus, dockypath)
		self.bus.add_signal_receiver (self.item_removed, "ItemRemoved",  dockyiface, dockybus, dockypath)
		self.bus.add_signal_receiver (self.shut_down,    "ShuttingDown", dockyiface, dockybus, dockypath)
		
		for pathtoitem in paths:
			obj = self.bus.get_object (dockybus, pathtoitem)
			item = dbus.Interface (obj, itemiface)
			if item.GetOwnsUri() or item.GetOwnsDesktopFile():
				self.items[pathtoitem] = DockyItem (pathtoitem)
	
	def item_added(self, path):
		if self.disposed:
			return;
		obj = self.bus.get_object (dockybus, path)
		item = dbus.Interface (obj, itemiface)
		if item.GetOwnsUri() or item.GetOwnsDesktopFile ():
			self.items[path] = DockyItem (path)

	def item_removed(self, path):
		if self.disposed:
			return;
		if path in self.items:
			self.items[path].dispose ()
			del self.items[path]
	
	def shut_down(self):
		self.dispose ()
		gobject.idle_add (quit, 1)
	
	def dispose(self):
		self.disposed = True;
		for path, item in self.items.iteritems ():
			item.dispose ()
			
	
if __name__ == "__main__":
	dockysink = DockySink()
	mainloop = gobject.MainLoop(is_running=True)

	while mainloop.is_running():
		print 'running'
		try:
		    mainloop.run()
		except KeyboardInterrupt:
		    dockysink.dispose ()
		    gobject.idle_add(quit, 1)
		print 'done\n\n'
