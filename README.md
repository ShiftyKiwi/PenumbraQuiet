# SILENCEPenumbrussy

SILENCEPenumbrussy suppresses Penumbra mod-complete toasts and selected error notifications.

## Troubleshooting notice

If you experience issues with Penumbra while using this plugin, first disable SILENCEPenumbrussy and retest. If the issue persists, report it to Penumbra. If it disappears, please report the issue to ShiftyKiwi on GitHub.

## Custom repo URL

Add this to the Experimental repo URLs list in `/xlsettings`:

`https://raw.githubusercontent.com/ShiftyKiwi/SILENCEPenumbrussy/main/pluginmaster.json`

## Settings

Open settings with `/silencepenumbrussy`.

* Enable filtering: master toggle.
* Suppress mod-complete toasts: hides the mod-complete toast on spawn.
* Match Penumbra source only: targets only notifications initiated by Penumbra when available.
* Require both import and extract text: stricter text match to avoid false positives.
* Suppress error toasts: hides Penumbra error toasts like Forbidden File Redirection or Collection without ID found.
* Automatically prune error messages from Penumbra's Messages tab.
