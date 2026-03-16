# SILENCEPenumbrussy

SILENCEPenumbrussy suppresses Penumbra mod-complete toasts and selected error notifications.

## Troubleshooting Notice

By using this plugin, you acknowledge that certain Penumbra error messages
and notifications may be intentionally hidden.

If you encounter issues with your mods, disable SILENCEPenumbrussy and
reproduce the issue before requesting support in the Penumbra Discord server.

Many problems reported to Penumbra developers are normally visible through
error toasts or message logs that this plugin may suppress. Please verify
that the issue still occurs with this plugin disabled before requesting support.

If the issue disappears after disabling SILENCEPenumbrussy, it may be related
to this plugin. In that case, please report the issue on this repository's
GitHub issue tracker.

## Custom repo URL

Add this to the Experimental repo URLs list in `/xlsettings`:

`https://raw.githubusercontent.com/ShiftyKiwi/SILENCEPenumbrussy/main/pluginmaster.json`

## Settings

Open settings with `/silencepenumbrussy`.

* Enable filtering: master toggle.
* Suppress mod-complete toasts: hides the mod-complete toast on spawn.
* Match Penumbra source only: targets only notifications initiated by Penumbra when available.
* Require both import and extract text: stricter text match to avoid false positives.
* Suppress error toasts: hides Penumbra error toasts like Forbidden File Encountered / Forbidden File Redirection or Collection without ID found.
* Automatically prune error messages logged in Penumbra's Messages tab.
