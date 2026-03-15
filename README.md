# SILENCEPenumbrussy

SILENCEPenumbrussy suppresses or auto-dismisses Penumbra mod import completion notifications.

## Build

1. Open `SILENCEPenumbrussy.sln` in Visual Studio or Rider.
2. Build the solution (Debug or Release).
3. The DLL will be at `SILENCEPenumbrussy/bin/x64/<Config>/SILENCEPenumbrussy.dll`.

## Use in game

1. Add the DLL path to Dalamud dev plugins in `/xlsettings` under Experimental.
2. Enable the plugin in `/xlplugins` under Dev Tools > Installed Dev Plugins.
3. Open settings with `/silencepenumbrussy`.

## Custom repo URL

Add this to the Experimental repo URLs list in `/xlsettings`:

`https://raw.githubusercontent.com/ShiftyKiwi/SILENCEPenumbrussy/main/pluginmaster.json`

## Settings

* Enable filtering: master toggle.
* Suppress immediately: hides the mod-complete toast on spawn.
* Auto-dismiss after: dismisses after N seconds.
* Match Penumbra source only: targets only notifications initiated by Penumbra when available.
* Require both import and extract text: stricter text match to avoid false positives.

## Patch alignment

The manifest `ApplicableVersion` is currently set to the SupportedGameVer in your local XIVLauncher hooks.
Update it after each hotfix if needed.
