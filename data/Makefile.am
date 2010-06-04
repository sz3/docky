SUBDIRS = icons themes

EXTRA_DIST = docky.desktop.in \
	remaps.ini \
	sun.svg \
	wind.svg \
	temp.svg \
	poof.png
CLEANFILES = docky.desktop

applicationsdir = $(datadir)/applications
applications_DATA = docky.desktop

startupdir = $(sysconfdir)/xdg/autostart
startup_DATA = docky.desktop

remapsdir = $(datadir)/docky
remaps_DATA = remaps.ini

poofdir = $(datadir)/docky
poof_DATA = poof.png \
	sun.svg \
	wind.svg \
	temp.svg

@INTLTOOL_DESKTOP_RULE@
