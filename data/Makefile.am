SUBDIRS = icons themes

EXTRA_DIST = docky.desktop.in \
	sun.svg \
	wind.svg \
	temp.svg \
	poof.png
CLEANFILES = docky.desktop

applicationsdir = $(datadir)/applications
applications_DATA = docky.desktop

startupdir = $(sysconfdir)/xdg/autostart
startup_DATA = docky.desktop

poofdir = $(datadir)/docky
poof_DATA = poof.png \
	sun.svg \
	wind.svg \
	temp.svg

@INTLTOOL_DESKTOP_RULE@
