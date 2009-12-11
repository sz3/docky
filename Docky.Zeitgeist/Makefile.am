# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Zeitgeist
TARGET = library

FILES = \
	Zeitgeist/ZeitgeistResult.cs \
	Zeitgeist/ZeitgeistFilter.cs \
	Zeitgeist/ZeitgeistProxy.cs

PROJECT_REFERENCES= \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS)
