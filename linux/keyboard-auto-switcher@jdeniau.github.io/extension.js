// SPDX-License-Identifier: MIT
// Keyboard Auto Switcher: switch to US Dvorak when a TypeMatrix keyboard
// is plugged in, and back to French AZERTY when it is removed.

import Gio from "gi://Gio";
import GLib from "gi://GLib";
import GUdev from "gi://GUdev";

import {Extension} from "resource:///org/gnome/shell/extensions/extension.js";
import * as Main from "resource:///org/gnome/shell/ui/main.js";
import {getInputSourceManager, INPUT_SOURCE_TYPE_XKB} from "resource:///org/gnome/shell/ui/status/keyboard.js";

// TypeMatrix 2030 USB identifiers (VID_1E54 / PID_2030, same as the Windows version)
const TARGET_KEYBOARD = {vendor: "1e54", product: "2030"};

// xkb ids, as listed by: gsettings get org.gnome.desktop.input-sources sources
const LAYOUT_WITH_KEYBOARD = "us+dvorak";
const LAYOUT_WITHOUT_KEYBOARD = "fr+azerty";

// Compose key for each case. On the TypeMatrix (US Dvorak), right Alt is free, so it
// becomes Compose. On the laptop (French AZERTY), right Alt must stay AltGr for ~ # { [ | @…,
// so Compose goes on right Ctrl instead. (The Copilot key cannot be used: it sends a
// Shift+Super+F23 chord that no xkb option can catch.) Other xkb options are left untouched.
const COMPOSE_WITH_KEYBOARD = "compose:ralt";
const COMPOSE_WITHOUT_KEYBOARD = "compose:rctrl";

// A single plug/unplug emits several uevents (device, hub, interfaces): debounce them
const SETTLE_DELAY_MS = 500;

export default class KeyboardAutoSwitcher extends Extension {
    enable() {
        this._inputSettings = new Gio.Settings({schema_id: "org.gnome.desktop.input-sources"});
        this._udev = new GUdev.Client({subsystems: ["usb/usb_device"]});
        this._ueventId = this._udev.connect("uevent", (_client, action, _device) => {
            if (action === "add" || action === "remove")
                this._scheduleCheck();
        });

        // Initial state at login. Thanks to "session-modes" in metadata.json the
        // extension also stays active on the lock screen, so a keyboard plugged or
        // unplugged while the session is locked is handled before the password is typed.
        this._check();
    }

    disable() {
        if (this._timeoutId) {
            GLib.Source.remove(this._timeoutId);
            this._timeoutId = 0;
        }
        this._udev.disconnect(this._ueventId);
        this._udev = null;
        this._inputSettings = null;
    }

    _applyComposeKey(connected) {
        const wanted = connected ? COMPOSE_WITH_KEYBOARD : COMPOSE_WITHOUT_KEYBOARD;
        const current = this._inputSettings.get_strv("xkb-options");
        const next = [...current.filter(option => !option.startsWith("compose:")), wanted];
        if (next.join(",") !== current.join(","))
            this._inputSettings.set_strv("xkb-options", next);
    }

    _isTargetKeyboardConnected() {
        return this._udev.query_by_subsystem("usb").some(device =>
            device.get_devtype() === "usb_device" &&
            device.get_sysfs_attr("idVendor") === TARGET_KEYBOARD.vendor &&
            device.get_sysfs_attr("idProduct") === TARGET_KEYBOARD.product);
    }

    _scheduleCheck() {
        if (this._timeoutId)
            GLib.Source.remove(this._timeoutId);

        this._timeoutId = GLib.timeout_add(GLib.PRIORITY_DEFAULT, SETTLE_DELAY_MS, () => {
            this._timeoutId = 0;
            this._check();
            return GLib.SOURCE_REMOVE;
        });
    }

    _check() {
        const connected = this._isTargetKeyboardConnected();
        this._applyComposeKey(connected);

        const wantedId = connected ? LAYOUT_WITH_KEYBOARD : LAYOUT_WITHOUT_KEYBOARD;

        const manager = getInputSourceManager();
        const current = manager.currentSource;
        if (current?.type === INPUT_SOURCE_TYPE_XKB && current.id === wantedId)
            return;

        const target = Object.values(manager.inputSources)
            .find(source => source.type === INPUT_SOURCE_TYPE_XKB && source.id === wantedId);
        if (!target) {
            console.warn(`[keyboard-auto-switcher] "${wantedId}" is not among the configured input sources`);
            return;
        }

        // interactive = true: GNOME persists the choice in mru-sources, like a manual switch
        target.activate(true);

        // The lock screen already shows the layout indicator in its top bar
        if (Main.sessionMode.isLocked)
            return;
        Main.notify(
            connected ? "Clavier TypeMatrix branché" : "Clavier TypeMatrix débranché",
            `Disposition : ${target.displayName}`);
    }
}
