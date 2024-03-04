del /s /q Build 
dotnet publish YAFC/YAFC.csproj -r win-x64 -c Release -o Build/Windows
dotnet publish YAFC/YAFC.csproj -r osx-x64 --self-contained true -c Release -o Build/OSX
dotnet publish YAFC/YAFC.csproj -r linux-x64 --self-contained true -c Release -o Build/Linux

cd Build
%SystemRoot%\System32\tar.exe -czf Linux.tar.gz Linux
%SystemRoot%\System32\tar.exe -czf OSX.tar.gz OSX
powershell Compress-Archive Windows Windows.zip

pause;

