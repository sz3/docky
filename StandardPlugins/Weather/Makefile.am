# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Weather

FILES = \
	src/ForecastMenuItem.cs \
	src/WeatherForecast.cs \
	src/IWeatherSource.cs \
	src/Sources/AbstractWeatherSource.cs \
	src/Sources/WeatherChannelWeatherSource.cs \
	src/Sources/GoogleWeatherSource.cs \
	src/Sources/WunderWeatherSource.cs \
	src/WeatherItemProvider.cs \
	src/WeatherConfiguration.cs \
	src/WeatherConfigurationDialog.cs \
	src/WeatherPreferences.cs \
	src/WeatherController.cs \
	src/WeatherService.cs \
	src/WeatherDocklet.WeatherConfiguration.cs \
	src/WeatherUnits.cs \
	src/WeatherDocklet.cs \
	src/WeatherPainter.cs \
	src/generated.cs \
	src/WeatherErrorArgs.cs

RESOURCES = \
	Resources/Weather.addin.xml

REFERENCES =  \
	System \
	System.Core \
	System.Web \
	System.Xml \
	Mono.Cairo \
	Mono.Posix \
	$(DOCKY_CAIRO_LIBS) \
	$(DOCKY_SERVICES_LIBS) \
	$(DOCKY_ITEMS_LIBS) \
	$(GTK_SHARP_20_LIBS)
