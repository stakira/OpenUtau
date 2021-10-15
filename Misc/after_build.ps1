((Get-Content -path .\Misc\release.xml -Raw) -replace '0.0.0.0', $env:APPVEYOR_BUILD_VERSION) | Set-Content -Path .\release.xml
git log master -8 --no-merges --pretty=format:'%cd %s %b' --date=format:'%Y-%m-%d' > changelog.txt
