# JotunnModExample
Example Valheim mod built with [Jötunn](https://github.com/Valheim-Modding/Jotunn).
Most of our [Tutorials](https://valheim-modding.github.io/Jotunn/tutorials/overview.html) refer to this examples.

## Building
How to setup the development enviroment for this project.

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com) and add the C# workload.
2. Download this package: [BepInEx pack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
3. Unpack and copy the contents of `BepInExPack_Valheim` into your Valheim root folder. You should now see a new folder called `<ValheimDir>\unstripped_corlib` and more additional stuff.
4. Clone this repository using git.
5. Open the Solution file `<JotunnModExample>\JotunnModExample.sln` and build the project

A new environment file `Environment.props` can be created in the projects base path `<JotunnModExample>`.
Make sure you are not in any subfolder.
Paste this snippet and change the paths accordingly.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Valheim install folder. This is normally found automatically, uncomment to overwrite it. Needs to be your path to the base Valheim folder. -->
    <!-- <VALHEIM_INSTALL>X:\PathToYourSteamLibary\steamapps\common\Valheim</VALHEIM_INSTALL>-->

    <!-- This is the folder where your build gets copied to when using the post-build automations -->
    <MOD_DEPLOYPATH>$(VALHEIM_INSTALL)\BepInEx\plugins</MOD_DEPLOYPATH>
  </PropertyGroup>
</Project>
```
