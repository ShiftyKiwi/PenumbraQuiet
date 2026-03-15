# SILENCEPenumbrussy

SILENCEPenumbrussy suppresses Penumbra mod import toasts and selected error notifications.

## Custom repo URL

Add this to the Experimental repo URLs list in `/xlsettings`:

`https://raw.githubusercontent.com/ShiftyKiwi/SILENCEPenumbrussy/main/pluginmaster.json`

## Settings

Open settings with `/silencepenumbrussy`.

* Enable filtering: master toggle.
* Suppress mod import toasts: hides import progress and completion toasts.
* Match Penumbra source only: targets only notifications initiated by Penumbra when available.
* Require both import and extract text: stricter text match to avoid false positives.
* Suppress error toasts: hides Penumbra error toasts like Forbidden File Redirection or Collection without ID found.
* Remove from Penumbra messages tab: deletes matching errors from the Messages tab.
