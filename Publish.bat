set targets=win-x86 win-arm linux-x64 linux-arm osx-x64
(for %%a in (%targets%) do (
    dotnet publish %~dp0\FNTTools -r %%a -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true || exit
    mkdir release\%%a\
    copy %~dp0\FNTTools\bin\Release\netcoreapp3.1\%%a\publish\FNTTools.exe release\%%a\FNTTools.exe
    copy %~dp0\FNTTools\bin\Release\netcoreapp3.1\%%a\publish\FNTTools release\%%a\FNTTools
    copy %~dp0\LICENSE release\%%a\LICENSE
))