# SAT2IP Gui Application
## Introduction
This application can play Satellite streams from a Sat2Ip server.
It was tested with a KATHREIN EXIP 414/E. It has 4 tuners. Currently always tuner 4 is used.

You need an OSCAM server to view channels, even if they are not encrypted. An OSCAM docker container  can be built from https://github.com/tgiesela/tvheadend.

You also need VLC which can be downloaded from https://www.videolan.org/vlc/index.html

## Development
During development the FFDecsa.dll needs to be copied to the projects output directory. Otherwise the DLL cannot be found.
Also when you need console debugging, set the Projects output to Console application instead of Windows Application.
Debug file logging default goes to %APPDATA%\Local\Temp.
