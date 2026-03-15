# SILENCEPenumbrussy

SILENCEPenumbrussy suppresses or auto-dismisses Penumbra mod import completion notifications.

## Custom repo URL

Add this to the Experimental repo URLs list in `/xlsettings`:

`https://raw.githubusercontent.com/ShiftyKiwi/SILENCEPenumbrussy/main/pluginmaster.json`

## Settings

Open settings with `/silencepenumbrussy`.

* Enable filtering: master toggle.
* Suppress immediately: hides the mod-complete toast on spawn.
* Auto-dismiss after: dismisses after N seconds.
* Match Penumbra source only: targets only notifications initiated by Penumbra when available.
* Require both import and extract text: stricter text match to avoid false positives.
