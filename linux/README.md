# Keyboard Auto Switcher for Linux (GNOME Shell extension)

Same behaviour as the Windows application, implemented as a GNOME Shell extension:
when a TypeMatrix keyboard (USB `1e54:2030`) is plugged in, GNOME switches to
**US Dvorak**; when it is removed, GNOME switches back to **French AZERTY**.

Tested against GNOME Shell 50 on Fedora 44 (Wayland).

## Why an extension rather than a daemon?

On GNOME Wayland there is no public D-Bus API to change the current input source,
and the `org.gnome.desktop.input-sources current` gsettings key is deprecated and
ignored by the Shell. Only code running inside GNOME Shell can call
`getInputSourceManager().inputSources[i].activate()`. Running inside the Shell
also gives us for free what the Windows version had to implement by hand:

| Windows version                         | GNOME extension                                              |
| --------------------------------------- | ------------------------------------------------------------ |
| WMI `Win32_USBHub` event watcher        | GUdev `uevent` signal on the `usb` subsystem                  |
| `ActivateKeyboardLayout` (user32)       | `InputSource.activate()` from the Shell                      |
| Resume from sleep handler               | USB devices re-enumerate on resume and emit uevents          |
| Session unlock handler                  | Extension stays active on the lock screen (`session-modes`)   |
| System tray icon with current layout    | GNOME already shows the current input source in the top bar  |
| "Launch at startup" registry entry      | Enabled extensions load with the session                     |
| Log viewer                              | `journalctl --user -f -o cat /usr/bin/gnome-shell`            |

## Lock screen and logged-out session

`metadata.json` declares `"session-modes": ["user", "unlock-dialog"]`, so GNOME keeps
the extension running while the session is locked. Plugging or unplugging the
TypeMatrix on the lock screen switches the layout immediately, before the password
is typed. Without this, GNOME would disable the extension on lock and only re-run
the check after a successful unlock.

When the session is closed (logged out), nothing runs. The initial check in
`enable()` handles whatever is plugged in at the next login.

## Requirements

- GNOME Shell 50 (bump `shell-version` in `metadata.json` for other releases).
- Both layouts configured in Settings > Keyboard > Input Sources. Check with:

```sh
gsettings get org.gnome.desktop.input-sources sources
# expected to contain: (xkb, fr+azerty) and (xkb, us+dvorak)
```

## Install

```sh
./linux/install.sh
```

This symlinks the extension into `~/.local/share/gnome-shell/extensions/` and
enables it. On Wayland, GNOME Shell only loads new extensions at login, so log out
and back in once. Afterwards, edits to `extension.js` are picked up at the next login.

Uninstall with `./linux/uninstall.sh`.

## Configuration

Everything is hardcoded at the top of `extension.js`, like in the Windows version:

- `TARGET_KEYBOARD`: USB vendor/product ids (`lsusb` shows them as `1e54:2030`).
- `LAYOUT_WITH_KEYBOARD` / `LAYOUT_WITHOUT_KEYBOARD`: xkb ids as listed by `gsettings`.
- `SETTLE_DELAY_MS`: debounce, since one plug/unplug emits several uevents.

## Debugging

```sh
# extension state and last error, if any
gnome-extensions info keyboard-auto-switcher@jdeniau.github.io

# live logs (warnings from the extension are prefixed with [keyboard-auto-switcher])
journalctl --user -f -o cat /usr/bin/gnome-shell

# current input source, as persisted by GNOME after each switch
gsettings get org.gnome.desktop.input-sources mru-sources
```

To iterate without logging out, GNOME 50 can run a nested development session with
`gnome-shell --devkit`.
