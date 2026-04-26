# CoreIsland

use msbuild to generate an AOT publish

`msbuild .\App1.csproj /t:Publish /p:PublishProfile=win-x64 /p:Platform=x64 /p:Configuration=Release`

`msbuild .\App1.csproj /t:Publish /p:PublishAot=true /p:SelfContained=true /p:RuntimeIdentifier=win-x64 /p:Platform=x64 /p:Configuration=Release`
