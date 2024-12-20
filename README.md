# Danish GeoJSON extractor

Downloads and extracts GeoJSON from a specific set of Danish sources.

## Usage

Move the `appsettings_copy.json` to the same folder as the program and rename it to `appsettings.json`. Go into the file and enable the data-sources you need.

## Note

By default it looks for the `appsettings.json` file in same folder as the binary file. This can be overwritten by calling the binary file with a full path to the file.

Example:

```sh
./my_executeable "/home/my_user/appsettings.json"
```

## Datafordeler

When using data from 'Datafordeleren' it is required to setup the `ftpSetting` in the `appsettings.json` file. The setup is done on the website [datafordeler.dk](https://datafordeler.dk/). The setup of the specific datasets unique to 'Datafordeleren' is described in the sections below.

This guide shows how to setup "datafordeler" predefined datasets [link.](/docs/guide.md)

### Using Matrikel

When using data from the `matrikel` section you've to subscribe to the predefined dataset named `MAT Kortdata Gældende DK Complete GPKG`.

### Using GeoDanmark

When using data from the `geoDanmark` section you've to subscribe to the predefined dataset named `GeoDanmark60_GML_HF`.

### Using StedNavn

When using data from the `stedNavn` section you've to subscribe to the predefined dataset named `DKstednavneBearbejdedeNohist_GML321`.

## Dependencies

Has dependency on GDAL (ogr2ogr), it is required to be in the shell path.

## Release build

Publish for Linux.

```sh
dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained false -o ./build
```

Publish for Windows.

```sh
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained false -o ./build
```
