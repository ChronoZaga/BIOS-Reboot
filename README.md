# BIOS Reboot

A small Windows app that restarts your PC straight into the BIOS/UEFI setup screen, so you don't have to press a key at the right moment during boot.

## Usage

1. Run `BIOS Reboot.exe`.
2. Approve the admin (UAC) prompt.
3. Click **Yes** twice to confirm.

The PC restarts immediately into firmware setup. **No** is the default button, so pressing Enter by accident won't restart the PC.

## Requirements

- Windows 10 or 11, 64-bit
- A UEFI-based system (legacy BIOS can't be rebooted into setup this way)
- Administrator rights

## Building

Run `BIOS Reboot Build.bat`. It compiles `BIOS Reboot.cs` into a 64-bit `BIOS Reboot.exe` using the C# compiler that comes with Windows (.NET Framework 4.x). Nothing else needs to be installed. If `BIOS Reboot.ico` is in the same folder, it's used as the app icon.

## How it works

It runs `shutdown /r /fw /t 0` (restart into firmware setup with no delay).

## License

Released under the [MIT License](LICENSE).
