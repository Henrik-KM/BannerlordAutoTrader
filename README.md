# BannerlordAutoTrader
AutoTrader mod for Mount&amp;Blade II: Bannerlord

## Installation
You can download and install the mod using:
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2875660601)
- [Nexusmods](https://www.nexusmods.com/mountandblade2bannerlord/mods/135)

## Building from source

The repository now includes an MSBuild project so you can produce the same layout that ships with the workshop release.

1. Install the [.NET SDK 8.0 or later](https://dotnet.microsoft.com/download) and ensure the `dotnet` CLI is available on your PATH.
2. Restore and build the module in Release mode:
   ```bash
   dotnet build AutoTrader/AutoTrader.csproj -c Release
   ```
3. Copy the contents of `AutoTrader/bin/Win64_Shipping_Client/` into your `Mount & Blade II Bannerlord/Modules/AutoTrader/bin/Win64_Shipping_Client/` folder (create the folders if they do not exist).

The project references the `MountAndBlade.ReferenceAssemblies` NuGet package so no manual game DLL copying is required.

## Contribution
Feel free to create a PR for any feature or fix you'd like to add. Once they get merged to main the workshop / nexusmods page will be updated.
