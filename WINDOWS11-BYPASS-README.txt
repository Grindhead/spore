Windows 11 unsupported hardware fallback
========================================

This USB has autounattend.xml at the root. When booting from the USB, Windows
Setup should automatically add LabConfig registry values to bypass TPM, Secure
Boot, RAM, storage, and CPU checks.

If Setup still says the PC cannot run Windows 11:

1. Press Shift+F10 to open Command Prompt.
2. Find the USB drive letter. It may not be E: inside Setup.
   Try:
     dir C:
     dir D:
     dir E:
     dir F:
   Look for bypass-windows11-requirements.cmd.
3. Run it, for example:
     E:\bypass-windows11-requirements.cmd
4. Close Command Prompt.
5. Go back one step in Windows Setup, then continue.

Important: this is still an unsupported Windows 11 installation. Back up files
before installing, and preferably install onto a spare SSD or empty partition.
