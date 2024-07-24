for /f %%i in ('findstr /r /C:"<AssemblyVersion>.*</AssemblyVersion>" .\Yafc\Yafc.csproj') do set ver2=%%i
set "ver1=%ver2:<AssemblyVersion>=%"
set "ver=%ver1:</AssemblyVersion>=%"

del /s /q Build 
dotnet publish Yafc/Yafc.csproj -r win-x64 -c Release -o Build/Windows
dotnet publish Yafc/Yafc.csproj -r osx-x64 --self-contained false -c Release -o Build/OSX
dotnet publish Yafc/Yafc.csproj -r linux-x64 --self-contained false -c Release -o Build/Linux

cd Build
%SystemRoot%\System32\tar.exe -czf Yafc-%ver%-Linux.tar.gz Linux
%SystemRoot%\System32\tar.exe -czf Yafc-%ver%-OSX.tar.gz OSX
powershell Compress-Archive Windows Yafc-%ver%-Windows.zip

pause;
