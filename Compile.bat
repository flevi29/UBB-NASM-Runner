@echo off
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained false -c Release -o "%~dp0\binary"
echo:
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -c Release -o "%~dp0\binary-standalone"
echo:
pause