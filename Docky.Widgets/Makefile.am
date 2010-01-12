# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Widgets
TARGET = library

PKG_CONFIG_FILES = docky.widgets.pc

FILES = \
	AssemblyInfo.cs \
	Docky.Widgets/AbstractTileObject.cs \
	Docky.Widgets/Actor.cs \
	Docky.Widgets/AnimatedBox.cs \
	Docky.Widgets/AnimatedHBox.cs \
	Docky.Widgets/AnimatedVBox.cs \
	Docky.Widgets/AnimatedWidget.cs \
	Docky.Widgets/Choreographer.cs \
	Docky.Widgets/ConfigDialog.cs \
	Docky.Widgets/ExtensionMethods.cs \
	Docky.Widgets/SingleActorStage.cs \
	Docky.Widgets/SearchEntry.cs \
	Docky.Widgets/HoverImageButton.cs \
	Docky.Widgets/Stage.cs \
	Docky.Widgets/Tile.cs \
	Docky.Widgets/TileView.cs \
	Docky.Widgets/WrapLabel.cs \
	Docky.Widgets/ListOnlyTileView.cs \
	Docky.Widgets/AbstractLoginWidget.cs \
	Docky.Widgets/IConfig.cs \
	gtk-gui/Docky.Widgets.AbstractLoginWidget.cs \
	gtk-gui/generated.cs

RESOURCES = \
	Resources/Docky.Widgets.addin.xml

PROJECT_REFERENCES= \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)

EXTRA_DIST += Docky.Widgets.dll.config

module_DATA += Docky.Widgets.dll.config

MCS_FLAGS += -unsafe
