SUBDIRS = Docky.DefaultClockTheme

# Simple component buildsystem
include $(top_srcdir)/build.rules.mk

ASSEMBLY = Docky.StandardPlugins
TARGET = library

PKG_CONFIG_FILES = docky.standardplugins.pc

FILES = \
	BatteryMonitor/BatteryMonitorItemProvider.cs \
	BatteryMonitor/BatteryMonitorProcItem.cs \
	Bookmarks/BookmarksItemProvider.cs \
	Bookmarks/BookmarkDockItem.cs \
	Clock/ClockItemProvider.cs \
	Clock/ClockDockItem.cs \
	Clock/ClockThemeSelector.cs \
	CPUMonitor/CPUMonitorDockItem.cs \
	CPUMonitor/CPUMonitorItemProvider.cs \
	GMail/GMailItemProvider.cs \
	GMail/GMailAtom.cs \
	GMail/GMailErrorArgs.cs \
	GMail/GMailLoginConfig.cs \
	GMail/GMailPreferences.cs \
	GMail/GMailConfiguration.cs \
	GMail/GMailConfigurationDialog.cs \
	GMail/GMailMenuItem.cs \
	GMail/GMailDockItem.cs \
	GMail/AbstractLoginWidget.cs \
	GMail/gtk-gui/GMail.GMailConfiguration.cs \
	GMail/gtk-gui/GMail.AbstractLoginWidget.cs \
	GMail/gtk-gui/generated.cs \
	Trash/TrashItemProvider.cs \
	Trash/TrashDockItem.cs \
	Mounter/MountItem.cs \
	Mounter/MountProvider.cs \
	Weather/ForecastMenuItem.cs \
	Weather/WeatherForecast.cs \
	Weather/IWeatherSource.cs \
	Weather/Sources/AbstractWeatherSource.cs \
	Weather/Sources/WeatherChannelWeatherSource.cs \
	Weather/Sources/GoogleWeatherSource.cs \
	Weather/Sources/WunderWeatherSource.cs \
	Weather/WeatherItemProvider.cs \
	Weather/WeatherConfiguration.cs \
	Weather/WeatherConfigurationDialog.cs \
	Weather/WeatherPreferences.cs \
	Weather/WeatherController.cs \
	Weather/WeatherService.cs \
	Weather/WeatherDocklet.WeatherConfiguration.cs \
	Weather/WeatherUnits.cs \
	Weather/WeatherDocklet.cs \
	Weather/WeatherErrorArgs.cs

RESOURCES = \
	Resources/Docky.StandardPlugins.addin.xml \
	Resources/icons/badge-yellow.svg \
	Resources/icons/gmail-logo-dark.png \
	Resources/icons/gmail-logo.png \
	Resources/battery/battery_bottom.svg \
	Resources/battery/battery_inside_10.svg \
	Resources/battery/battery_inside_100.svg \
	Resources/battery/battery_inside_20.svg \
	Resources/battery/battery_inside_30.svg \
	Resources/battery/battery_inside_40.svg \
	Resources/battery/battery_inside_50.svg \
	Resources/battery/battery_inside_60.svg \
	Resources/battery/battery_inside_70.svg \
	Resources/battery/battery_inside_80.svg \
	Resources/battery/battery_inside_90.svg \
	Resources/battery/battery_inside_90_new.svg \
	Resources/battery/battery_plugged.svg \
	Resources/battery/battery_top.svg

PROJECT_REFERENCES = \
	Docky.CairoHelper \
	Docky.Services \
	Docky.Items \
	Docky.Windowing

REFERENCES =  \
	System \
	System.Core \
	System.Xml \
	System.Web \
	Mono.Cairo \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(GNOME_VFS_SHARP_20_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(LIBRSVG_SHARP_LIBS)
