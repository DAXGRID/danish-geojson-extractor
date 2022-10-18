# Danish GeoJSON extractor

Downloads and extracts GeoJSON from a specific set of Danish sources.

## Usage

Move the `appsettings_copy.json` to the same folder as the program and rename it to `appsettings.json`. Go into the file and enable the data-sources you need.

When using data from `matrikel` or `geoDanmark`, setting up https://datafordeler.dk/ predefined data-sets `MatrikelGeometriGaeldendeDKComplete` and `GeoDanmark60_GML`.

## Note
Has dependency on GDAL (ogr2ogr), it is required to be in the shell path.
