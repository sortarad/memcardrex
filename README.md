# memcardrex
Advanced PlayStation 1 Memory Card editor. Now updated (January 2021) to work with modern operating system and cross-platorm on Windows 10 and MacOS.

## Supported Memory Card formats:

ePSXe/PSEmu Pro Memory Card(*.mcr)
DexDrive Memory Card(*.gme)
pSX/AdriPSX Memory Card(*.bin)
Bleem! Memory Card(*.mcd)
VGS Memory Card(*.mem, *.vgs)
PSXGame Edit Memory Card(*.mc)
DataDeck Memory Card(*.ddf)
WinPSM Memory Card(*.ps)
Smart Link Memory Card(*.psm)
MCExplorer(*.mci)
PSP virtual Memory Card(*.VMP) (opening only)
PS3 virtual Memory Card(*.VM1)
Supported single save formats:

PSXGame Edit single save(*.mcs)
XP, AR, GS, Caetla single save(*.psx)
Memory Juggler(*.ps1)
Smart Link(*.mcb)
Datel(.mcx;.pda)
RAW single saves
PS3 virtual saves (*.psv) (importing only)
Hardware interfaces
MemcardRex supports communication with the real Memory Cards via external devices:

## DexDrive
As you may or may not know DexDrive is a very quirky device and sometimes it just refuses to work.
Even the first party software (DexPlorer) has problems with it (failed detection of a device).
If you encounter problems, unplug power from DexDrive, unplug it from COM port and connect it all again.

It is recommended that a power cord is connected to DexDrive, otherwise some cards won't be detected.
Communication was tested on Windows 7 x64 on a real COM port and with a Prolific and FTDI based USB adapters.

To select a COM port DexDrive is connected to go to "Options"->"Preferences".

## MemCARDuino
MemCARDuino is an open source Memory Card communication software for various Arduino boards.

## PS1CardLink
PS1CardLink is a software for the actual PlayStation and PSOne consoles.
It requires an official or home made TTL serial cable for communication with PC.

With it your console becomes a Memory Card reader similar to the DexDrive and MemCARDuino.

Credits
Beta testers:
Gamesoul Master, Xtreme2damax and Carmax91.

Thanks to:
@ruantec, Cobalt, TheCloudOfSmoke, RedawgTS, Hard core Rikki, RainMotorsports, Zieg, Bobbi, OuTman, Kevstah2004, Kubusleonidas, Frédéric Brière, Mark James, Cor'e and DeadlySystem.
