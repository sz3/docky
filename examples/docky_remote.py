#!/usr/bin/env python

import dbus
import dbus.glib
import gobject
import glib

array = []

def menu_pressed_signal(id):
	print 'Signal: Menu Pressed'

def elapsed_timer():
	for x in array:
		banshee.ConfirmItem (x)
	return True;

bus = dbus.SessionBus ()

dockypath = '/org/gnome/Docky'
dockybus = 'org.gnome.Docky'
dockyiface = 'org.gnome.Docky'
itemiface = 'org.gnome.Docky.Item'

obj = bus.get_object (dockybus, dockypath)
docky = dbus.Interface (obj, dockyiface)

bansheepath = docky.DockItemPathForDesktopID ("banshee-1")

obj = bus.get_object (dockybus, bansheepath)
banshee = dbus.Interface (obj, itemiface)

play_id = banshee.AddMenuItem ("Play", "media-playback-start", "Banshee-Control")
array.append (play_id)

bus.add_signal_receiver (menu_pressed_signal, "MenuItemActivated", itemiface, dockybus, bansheepath)

glib.timeout_add (1000 * 60 * 2, elapsed_timer)

gobject.threads_init()
dbus.glib.init_threads()
main_loop = gobject.MainLoop()
main_loop.run()
