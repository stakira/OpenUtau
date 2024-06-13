import os
import sys
from datetime import datetime

appcast_ver = os.environ.get('APPVEYOR_BUILD_VERSION')


def write_appcast(appcast_os, appcast_rid, appcast_file):

    xml = '''<?xml version="1.0" encoding="utf-8"?>
<rss version="2.0" xmlns:sparkle="http://www.andymatuschak.org/xml-namespaces/sparkle">
<channel>
    <title>OpenUtau</title>
    <language>en</language>
    <item>
    <title>OpenUtau %s</title>
    <pubDate>%s</pubDate>
    <enclosure url="https://github.com/stakira/OpenUtau/releases/download/build%%2F%s/%s"
                sparkle:version="%s"
                sparkle:shortVersionString="%s"
                sparkle:os="%s"
                type="application/octet-stream"
                sparkle:signature="" />
    </item>
</channel>
</rss>''' % (appcast_ver, datetime.now().strftime("%a, %d %b %Y %H:%M:%S %z"),
             appcast_ver, appcast_file, appcast_ver, appcast_ver, appcast_os)

    with open("appcast.%s.xml" % (appcast_rid), 'w') as f:
        f.write(xml)


if sys.platform == 'win32':
    if appcast_ver is not None:
        os.system("git tag build/%s 2>&1" % (appcast_ver))
        os.system("git push origin build/%s 2>&1" % (appcast_ver))

    os.system("del *.xml 2>&1")

    import urllib.request
    urllib.request.urlretrieve("https://www.nuget.org/api/v2/package/Microsoft.AI.DirectML/1.12.0", "Microsoft.AI.DirectML.nupkg")
    os.system("mkdir Microsoft.AI.DirectML")
    os.system("tar -xf Microsoft.AI.DirectML.nupkg -C Microsoft.AI.DirectML")

    os.system("dotnet restore OpenUtau -r win-x86")
    os.system(
        "dotnet publish OpenUtau -c Release -r win-x86 --self-contained true -o bin/win-x86")
    os.system("copy /y OpenUtau.Plugin.Builtin\\bin\\Release\\netstandard2.1\\OpenUtau.Plugin.Builtin.dll bin\\win-x86")
    write_appcast("windows", "win-x86", "OpenUtau-win-x86.zip")

    os.system("dotnet restore OpenUtau -r win-x64")
    os.system(
        "dotnet publish OpenUtau -c Release -r win-x64 --self-contained true -o bin/win-x64")
    os.system("copy /y OpenUtau.Plugin.Builtin\\bin\\Release\\netstandard2.1\\OpenUtau.Plugin.Builtin.dll bin\\win-x64")
    write_appcast("windows", "win-x64", "OpenUtau-win-x64.zip")

    os.system("makensis -DPRODUCT_VERSION=%s OpenUtau.nsi" % (appcast_ver))
    write_appcast("windows", "win-x64-installer", "OpenUtau-win-x64.exe")

elif sys.platform == 'darwin':
    os.system("rm *.dmg")
    os.system("rm *.xml")

    os.system("git checkout OpenUtau/OpenUtau.csproj")
    os.system("rm LICENSE.txt")
    os.system(
        "sed -i '' \"s/0.0.0/%s/g\" OpenUtau/OpenUtau.csproj" % (appcast_ver))
    os.system("dotnet restore OpenUtau -r osx.10.14-x64")
    os.system("dotnet msbuild OpenUtau -t:BundleApp -p:Configuration=Release -p:RuntimeIdentifier=osx.10.14-x64 -p:UseAppHost=true -p:OutputPath=../bin/osx-x64/")
    os.system(
        "cp OpenUtau/Assets/OpenUtau.icns bin/osx-x64/publish/OpenUtau.app/Contents/Resources/")
    os.system("rm *.dmg")
    os.system("npm install -g create-dmg")
    os.system("create-dmg bin/osx-x64/publish/OpenUtau.app")
    os.system("mv *.dmg OpenUtau-osx-x64.dmg")
    os.system("codesign -fvs - OpenUtau-osx-x64.dmg")
    os.system("git checkout OpenUtau/OpenUtau.csproj")
    os.system("git checkout LICENSE.txt")

    write_appcast("macos", "osx-x64", "OpenUtau-osx-x64.dmg")

else:
    os.system("rm *.xml")

    os.system("dotnet restore OpenUtau -r linux-x64")
    os.system(
        "dotnet publish OpenUtau -c Release -r linux-x64 --self-contained true -o bin/linux-x64")
    os.system("chmod +x bin/linux-x64/OpenUtau")
    os.system("tar -C bin/linux-x64 -czvf OpenUtau-linux-x64.tar.gz .")
    write_appcast("linux", "linux-x64", "OpenUtau-linux-x64.tar.gz")
