((Get-Content -path .\Misc\release.xml -Raw) -replace '0.0.0.0', $env:APPVEYOR_BUILD_VERSION) | Set-Content -Path .\release.xml
