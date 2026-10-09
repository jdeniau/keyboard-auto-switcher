#!/usr/bin/env bash
# Install the GNOME Shell extension for the current user (as a symlink, so edits
# in this repo are picked up at the next login) and enable it.
set -euo pipefail

UUID="keyboard-auto-switcher@jdeniau.github.io"
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/$UUID"
DEST="$HOME/.local/share/gnome-shell/extensions/$UUID"

if [ -e "$DEST" ] && [ ! -L "$DEST" ]; then
    echo "$DEST already exists and is not a symlink, remove it first." >&2
    exit 1
fi

mkdir -p "$(dirname "$DEST")"
ln -sfn "$SRC" "$DEST"

# gnome-extensions talks to the running Shell, which only discovers new extensions
# at login. Fall back to editing the gsettings list directly when it is not known yet.
if ! gnome-extensions enable "$UUID" 2>/dev/null; then
    python3 - "$UUID" <<"PY"
import sys
from gi.repository import Gio
uuid = sys.argv[1]
settings = Gio.Settings.new("org.gnome.shell")
enabled = settings.get_strv("enabled-extensions")
if uuid not in enabled:
    settings.set_strv("enabled-extensions", enabled + [uuid])
settings.set_strv("disabled-extensions", [u for u in settings.get_strv("disabled-extensions") if u != uuid])
Gio.Settings.sync()
PY
fi

echo "Installed: $DEST -> $SRC"
echo "Log out and back in so that GNOME Shell loads the extension (required on Wayland)."
