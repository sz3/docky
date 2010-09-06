EXTRA_DIST = config.rpath  COPYRIGHT

ACLOCAL_AMFLAGS = -I m4 -I m4/shamrock

SUBDIRS = \
	. \
	data \
	po \
	Docky.CairoHelper \
	Docky.Services \
	Docky.Widgets \
	Docky.Items \
	Docky.DBus \
	StandardPlugins \
	Docky \
	m4

# Get rid of the build dir on clean.
clean-local:
	rm -rf $(top_builddir)/build
