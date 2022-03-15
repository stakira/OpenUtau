import os
import sys

appcast_ver = os.environ['APPVEYOR_BUILD_VERSION']


def write_legacy(appcast_os, appcast_rid, appcast_file):
    xml = '''<?xml version="1.0" encoding="utf-8"?>
<item>
    <version>%s</version>
        <url>https://github.com/stakira/OpenUtau/releases/download/OpenUtau-Latest/OpenUtau.zip</url>
    <mandatory>false</mandatory>
</item>''' % (appcast_ver)

    with open("release.xml", 'w') as f:
        f.write(xml)


def write_appcast(appcast_os, appcast_rid, appcast_file):

    xml = '''<?xml version="1.0" encoding="utf-8"?>
<rss version="2.0" xmlns:sparkle="http://www.andymatuschak.org/xml-namespaces/sparkle">
<channel>
    <title>OpenUtau</title>
    <language>en</language>
    <item>
    <title>OpenUtau %s</title>
    <pubDate>Tue, 26 Oct 2021 21:01:52 -07:00</pubDate>
    <enclosure url="https://github.com/stakira/OpenUtau/releases/download/OpenUtau-Latest/%s"
                sparkle:version="%s"
                sparkle:shortVersionString="%s"
                sparkle:os="%s"
                type="application/octet-stream"
                sparkle:signature="" />
    </item>
</channel>
</rss>''' % (appcast_ver, appcast_file, appcast_ver, appcast_ver, appcast_os)

    with open("appcast.%s.xml" % (appcast_rid), 'w') as f:
        f.write(xml)


if sys.platform == 'win32':
    os.system("del *.xml")

    os.system("dotnet restore OpenUtau -r win-x86")
    os.system(
        "dotnet publish OpenUtau -c Release -r win-x86 --self-contained true -o bin/win-x86")
    write_appcast("windows", "win-x86", "OpenUtau-win-x86.zip")

    os.system("dotnet restore OpenUtau -r win-x64")
    os.system(
        "dotnet publish OpenUtau -c Release -r win-x64 --self-contained true -o bin/win-x64")
    write_appcast("windows", "win-x64", "OpenUtau-win-x64.zip")

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
    os.system("git checkout OpenUtau/OpenUtau.csproj")

    write_appcast("macos", "osx-x64", "OpenUtau-osx-x64.dmg")

else:
    os.system("rm *.xml")

    os.system("dotnet restore OpenUtau -r linux-x64")
    os.system(
        "dotnet publish OpenUtau -c Release -r linux-x64 --self-contained true -o bin/linux-x64")
    os.system("chmod +x bin/linux-x64/OpenUtau")
    os.system("tar -C bin/linux-x64 -czvf OpenUtau-linux-x64.tar.gz .")
    write_appcast("linux", "linux-x64", "OpenUtau-linux-x64.tar.gz")
