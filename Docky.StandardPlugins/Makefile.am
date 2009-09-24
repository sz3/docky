# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky.StandardPlugins
TARGET = library

PKG_CONFIG_FILES = docky.standardplugins.pc

FILES = \
	Trash/TrashItemProvider.cs \
	Trash/TrashDockItem.cs

RESOURCES = \
	Resources/Docky.StandardPlugins.addin.xml

PROJECT_REFERENCES = \
	Docky.CairoHelper \
	Docky.Services \
	Docky.Items

REFERENCES =  \
	System \
	System.Core \
	$(GTK_SHARP_20_LIBS)
