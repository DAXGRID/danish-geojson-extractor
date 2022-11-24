# Danish GeoJSON extractor

Downloads and extracts GeoJSON from a specific set of Danish sources.

## Usage

Move the `appsettings_copy.json` to the same folder as the program and rename it to `appsettings.json`. Go into the file and enable the data-sources you need.


## Datafordeler

When using data from 'Datafordeleren' it is required to setup the `ftpSetting` in the `appsettings.json` file. The setup is done on the website [datafordeler.dk](https://datafordeler.dk/). The setup of the specific datasets unique to 'Datafordeleren' is described in the sections below.

### Using Matrikel

When using data from the `matrikel` section you've to subscribe to the predefined dataset named `MatrikelGeometriGaeldendeDKComplete`.

### Using GeoDanmark

When using data from the `geoDanmark` section you've to subscribe to the predefined dataset named `GeoDanmark60_GML`.

### Using StedNavn

When using data from the `stedNavn` section you've to subscribe to the predefined dataset named `DKstednavneBearbejdedeNohist_GML321`.

## Dependencies

Has dependency on GDAL (ogr2ogr), it is required to be in the shell path.
