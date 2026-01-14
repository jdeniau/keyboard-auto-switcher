# Copilot Instructions for keyboard-auto-switcher

## Overview

Windows system tray app that auto-switches keyboard layouts (Dvorak ↔ AZERTY) when a TypeMatrix USB keyboard is connected/disconnected. Runs without admin privileges. Uses Velopack for auto-updates via GitHub Releases.

## 📚 Documentation

**For AI Assistants:** Read these files when needed for specific tasks:

- **`PROJECT-CONTEXT.md`** → Read first for quick project overview and common tasks
- **`ARCHITECTURE.md`** → Read for understanding components, patterns, and data flows
- **`DEVELOPMENT.md`** → Read for development workflow, testing, building, and how-tos
- **`TECHNICAL-DETAILS.md`** → Read for deep technical implementation details (WMI, Win32 APIs)

## When to Read What

- **Starting work on the project** → Read `PROJECT-CONTEXT.md`
- **Adding/modifying features** → Read `ARCHITECTURE.md` for component interactions
- **Debugging or testing** → Read `DEVELOPMENT.md` for troubleshooting and test patterns
- **Working with USB detection or keyboard APIs** → Read `TECHNICAL-DETAILS.md`

## Current State

- **Branch:** `keyboard-language-configuration` (likely adding configurable keyboards/layouts)
- **Limitations:** TypeMatrix keyboard only (hardcoded VID_1E54&PID_2030), Dvorak/AZERTY only
- **Logs:** `C:\ProgramData\KeyboardAutoSwitcher\logs\`
