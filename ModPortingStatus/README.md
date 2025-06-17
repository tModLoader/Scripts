# ModPortingStatus/
Scripts for creating a summary of how many mods have been ported to 1.4 TML.

### Requirements
- .NET
- `dotnet script` - Install via `dotnet tool install -g dotnet-script`.

### Usage
- Load data from `tmlapis.repl.co`: `dotnet script ./TmlModPortingStatus.csx`
- Load data from cached JSON files: `dotnet script ./TmlModPortingStatus.csx data.json`
