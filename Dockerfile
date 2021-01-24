FROM oiuldashov/avalonia-builder:2.0
ADD ./ /git/

WORKDIR /git/WixCreator
RUN msbuild WixCreator.sln -p:Configuration=Release
ARG BUILD_NET
RUN echo $BUILD_NET

RUN dotnet publish "/git/MemcardRex/MemcardRex.csproj" -c release -f net5.0 -r osx-x64 -p:Version=$BUILD_NET --self-contained true
RUN dotnet publish "/git/MemcardRex/MemcardRex.csproj" -c release -f net5.0 -r win-x64  -p:Version=$BUILD_NET --self-contained true


#Fix for .net core on linux bug (icon)
RUN node /git/bugfix_icon.js "/git/MemcardRex/bin/release/net5.0/win-x64/publish/MemcardRex.exe" "/git/MemcardRex/Assets/avalonia-logo.ico"

#Fix for .net core on linux bug (run as GUI app)
RUN python3 /git/bugfix_gui.py  "/git/MemcardRex/bin/release/net5.0/win-x64/publish/MemcardRex.exe"

#Build APP and DMG for OSX

#Copy to app content
#Copy to app content
RUN mkdir -p /git/Builds/App/


RUN mkdir -p /git/Builds/App/dmg/
RUN unzip -o -q /git/Builds/dmg.zip -d /git/Builds/dmg/
RUN cp -f -r /git/Builds/dmg/. /git/Builds/App/ 

RUN cp -R -f /git/MemcardRex/bin/release/net5.0/osx-x64/publish/* "/git/Builds/App/MemcardRex.app/Contents/MacOS/"

#Build DMG
WORKDIR /git/Builds/

RUN ln -s "/Applications" /git/Builds/App/Applications

RUN genisoimage -V "MemcardRex" -D -R -apple -no-pad -file-mode 0755 -o /tmp/MemcardRex_temp.dmg App/
RUN /usr/local/./dmg dmg /tmp/MemcardRex_temp.dmg /tmp/MemcardRex.dmg 
#Now DMG in /tmp/MemcardRex.dmg



#Build MSI for Windows

#Copy files for X64 MSI
RUN mkdir -p ~/.wine/drive_c/Build

RUN mkdir -p ~/.wine/drive_c/Build/MemcardRex
RUN mkdir -p ~/.wine/drive_c/Build/Wix 
RUN mkdir -p ~/.wine/drive_c/Build/WixCreator 

RUN cp -R /git/Builds/setup_icon.bmp "$HOME/.wine/drive_c/Build/setup_icon.bmp"
RUN cp -R /git/Builds/setup_background.bmp "$HOME/.wine/drive_c/Build/setup_background.bmp"


RUN cp -f "/git/Builds/licence-agreement.rtf" "$HOME/.wine/drive_c/Build/licence-agreement.rtf"
RUN cp -R /git/MemcardRex/bin/release/net5.0/win-x64/publish/* "$HOME/.wine/drive_c/Build/MemcardRex/"
RUN cp -R /git/WixCreator/WixCreator/bin/Release/* "$HOME/.wine/drive_c/Build/WixCreator/"
RUN cp -R /git/WixCreator/WixToolset/*  "$HOME/.wine/drive_c/Build/Wix/"


RUN rm -rf /git


#Create MSI for X64
WORKDIR /root/.wine/drive_c/Build/
RUN (timelimit xvfb-run -a wine "C:\Build\WixCreator\WixCreator.exe" "C:\Build\licence-agreement.rtf" "C:\Build\MemcardRex" ${BUILD_NET} "C:\Build\MemcardRex.msi" "C:\Build\Wix\bin"; exit 0)
RUN cp -f /root/.wine/drive_c/Build/MemcardRex.msi /tmp/MemcardRex.msi
#Now MSI in /root/.wine/drive_c/Build/MemcardRex.msi