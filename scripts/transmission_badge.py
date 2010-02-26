#!/usr/bin/env python

#  
#  Copyright (C) 2010 Dan Korostelev, Rico Tzschichholz
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
import dbus
import gobject

try:
	from docky.docky import DockyItem, DockySink
	from signal import signal, SIGTERM
	from sys import exit
	import urllib2
	import json
except ImportError, e:
	print e
	exit()

transmissionbus = "com.transmissionbt.Transmission"
transmissionrpcurl = "http://localhost:9091/transmission/rpc"
UPDATE_DELAY = 2000 # 2 secs


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


class DockyTransmissionItem(DockyItem):

	def __init__(self, path):
		DockyItem.__init__(self, path)
		self.request = urllib2.Request(transmissionrpcurl+'?method=session-stats')
		self.timer = 0

		self.bus.add_signal_receiver(self.name_owner_changed_cb,
		                             dbus_interface='org.freedesktop.DBus',
		                             signal_name='NameOwnerChanged',
		                             arg0=transmissionbus)
			
		self.start_polling()
		self.update_badge()

	def start_polling(self):
		if not self.timer > 0:
			self.timer = gobject.timeout_add(UPDATE_DELAY, self.update_badge)

	def stop_polling(self):
		if self.timer > 0:
			gobject.source_remove(self.timer)
			self.timer = 0

	def name_owner_changed_cb(self, name, old_owner, new_owner):
		if new_owner:
			# transmission started, resume polling
			self.start_polling()
			self.update_badge()
		else:
			# transmission stopped, stop polling and remove badge
			self.stop_polling()
			self.iface.ResetBadgeText()

	def update_badge(self):
		try:
			for i in range(2): # first try can be getting session id
				try:
					response = urllib2.urlopen(self.request)
					break # if no error, we don't need second try
				except urllib2.HTTPError, e:
					if e.code == 409 and 'X-Transmission-Session-Id' in e.headers:
						self.request.add_header('X-Transmission-Session-Id', e.headers['X-Transmission-Session-Id'])
			result = json.load(response)
			#Select Download Speed
			speed = result['arguments']['downloadSpeed']
			if speed:
				self.iface.SetBadgeText(bytes2ratestr(speed))
			else:
				self.iface.ResetBadgeText()
			return True
		except Exception as e:
			self.stop_polling()
			self.iface.ResetBadgeText()


class DockyTransmissionSink(DockySink):
	def item_path_found(self, pathtoitem, item):
		if item.GetOwnsDesktopFile() and item.GetDesktopFile().endswith("transmission.desktop"):
			self.items[pathtoitem] = DockyTransmissionItem(pathtoitem)

transmissionsink = DockyTransmissionSink()

def cleanup ():
	transmissionsink.dispose ()

if __name__ == '__main__':
	mainloop = gobject.MainLoop(is_running=True)
	
	atexit.register (cleanup)
	signal(SIGTERM, lambda signum, stack_frame: exit(1))
	
	mainloop.run()
