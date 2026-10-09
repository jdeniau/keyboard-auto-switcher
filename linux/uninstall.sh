#!/usr/bin/env bash
set -euo pipefail

UUID="keyboard-auto-switcher@jdeniau.github.io"

if ! gnome-extensions disable "$UUID" 2>/dev/null; then
    python3 - "$UUID" <<"PY"
import sys
from gi.repository import Gio
uuid = sys.argv[1]
settings = Gio.Settings.new("org.gnome.shell")
settings.set_strv("enabled-extensions", [u for u in settings.get_strv("enabled-extensions") if u != uuid])
Gio.Settings.sync()
PY
fi

rm -f "$HOME/.local/share/gnome-shell/extensions/$UUID"
echo "Removed. Log out and back in to fully unload it."
