((Get-Content -path .\Misc\release.xml -Raw) -replace '{{version}}', $env:APPVEYOR_BUILD_VERSION) | Set-Content -Path .\release.xml
((Get-Content -path .\Misc\appcast.xml -Raw) -replace '{{version}}', $env:APPVEYOR_BUILD_VERSION -replace '{{os}}', 'windows') | Set-Content -Path .\appcast.win.xml
((Get-Content -path .\Misc\appcast.xml -Raw) -replace '{{version}}', $env:APPVEYOR_BUILD_VERSION -replace '{{os}}', 'windows') | Set-Content -Path .\appcast.win-x86.xml
((Get-Content -path .\Misc\appcast.xml -Raw) -replace '{{version}}', $env:APPVEYOR_BUILD_VERSION -replace '{{os}}', 'windows') | Set-Content -Path .\appcast.win-x64.xml
git log master -8 --no-merges --pretty=format:'%cd %s %b' --date=format:'%Y-%m-%d' > changelog.txt
