# Sample custom connector implementation
This example uses local SQLite DB as a custom data source for [SeekTable](https://www.seektable.com) or [PivotData microservice](https://www.nrecosite.com/pivotdata_service.aspx).

## How to run example with PivotDataService

* ensure that you've downloaded PivotDataService binaries from the component page.
* in the command line change current folder to PivotDataService binaries location.
* execute `PivotDataServiceConfig\run.bat` from this example
* run custom connector web API: `dotnet run --urls=http://0.0.0.0:5001`
* open `localhost:5000' from your web browser