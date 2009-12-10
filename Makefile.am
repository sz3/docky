EXTRA_DIST = config.rpath  COPYRIGHT

ACLOCAL_AMFLAGS = -I m4 -I m4/shamrock

SUBDIRS = \
	. \
	data \
	scripts \
	lib \
	po \
	Docky.CairoHelper \
	Docky.Services \
	Docky.Widgets \
	Docky.Zeitgeist \
	Docky.Windowing \
	Docky.Items \
	Docky.DBus \
	StandardPlugins \
	Docky \
	m4

# Get rid of the build dir on clean.
clean-local:
	rm -rf $(top_builddir)/build
