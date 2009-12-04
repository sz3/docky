EXTRA_DIST = config.rpath  COPYRIGHT

ACLOCAL_AMFLAGS = -I m4 -I m4/shamrock

SUBDIRS = \
	. \
	data \
	lib \
	po \
	Docky.Services \
	Docky.Widgets \
	Docky.Zeitgeist \
	Docky.CairoHelper \
	Docky.Windowing \
	Docky.Items \
	StandardPlugins \
	Docky \
	m4

# Get rid of the build dir on clean.
clean-local:
	rm -rf $(top_builddir)/build
