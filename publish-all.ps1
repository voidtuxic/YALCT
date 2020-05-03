# requires 7zip or unix systems get all bad with subfolders
$7zipPath = "$env:ProgramFiles\7-Zip\7z.exe"

if (-not (Test-Path -Path $7zipPath -PathType Leaf)) {
    throw "7 zip file '$7zipPath' not found"
}

Set-Alias 7zip $7zipPath

New-Item -ItemType Directory -Force -Path .\build
cd YALCT
# Windows 64
dotnet publish -r win-x64 -c Release --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true
7zip a -tzip -mx=9 ..\build\YALCT-win64.zip .\bin\Release\netcoreapp3.1\win-x64\publish\* 
# OSX 64
dotnet publish -r osx-x64 -c Release --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true
7zip a -tzip -mx=9 ..\build\YALCT-osx64.zip .\bin\Release\netcoreapp3.1\osx-x64\publish\* 
# Linux 64
dotnet publish -r linux-x64 -c Release --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true
7zip a -tzip -mx=9 ..\build\YALCT-linux64.zip .\bin\Release\netcoreapp3.1\linux-x64\publish\* 
cd ..