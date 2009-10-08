EXTRA_DIST = COPYRIGHT

ACLOCAL_AMFLAGS = -I m4 -I m4/shamrock

SUBDIRS = \
	. \
	lib \
	Docky.Services \
	Docky.Zeitgeist \
	Docky.CairoHelper \
	Docky.Windowing \
	Docky.Items \
	Docky.StandardPlugins \
	Docky \
	m4

# Get rid of the build dir on clean.
clean-local:
	rm -rf $(top_builddir)/build
