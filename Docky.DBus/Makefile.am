# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.DBus
TARGET = library

FILES = \
	Docky.DBus/DBusManager.cs \
	Docky.DBus/DockyDBus.cs \
	Docky.DBus/DockyDBusItem.cs \
	Docky.DBus/IDockyDBus.cs \
	Docky.DBus/IDockyDBusItem.cs \
	Docky.DBus/RemoteItem.cs \
	Docky.DBus/RemoteMenuEntry.cs \
	Docky.DBus/RemoteDockItem.cs

PROJECT_REFERENCES = \
	Docky.Items \
	Docky.Windowing \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	$(GLIB_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS)
