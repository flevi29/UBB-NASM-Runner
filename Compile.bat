@echo off
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained false -c Release
echo:
xcopy /F /Y "%~dp0\UBB-NASM-Runner\bin\Release\netcoreapp3.1\win-x64\publish\UBB-NASM-Runner.exe" "C:\Users\F. Levi\RiderProjects\UBB-NASM-Runner\UBB-NASM-Runner.exe"
echo:
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -c Release
echo:
xcopy /F /Y "%~dp0\UBB-NASM-Runner\bin\Release\netcoreapp3.1\win-x64\publish\UBB-NASM-Runner.exe" "C:\Users\F. Levi\RiderProjects\UBB-NASM-Runner\UBB-NASM-Runner-standalone.exe"
echo:
pause