@ECHO OFF
@SET MSBUILDDIR=
@for /F "tokens=1,2*" %%i in ('reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v "MSBuildToolsPath"') DO (
    if "%%i"=="MSBuildToolsPath" (
        SET "MSBUILDDIR=%%k"
    )
)
@if "%MSBUILDDIR%"=="" exit /B 1
rmdir /S /Q aval\bin
%MSBUILDDIR%MSBuild.exe aval.sln /nologo /verbosity:minimal /t:Rebuild /p:Configuration=Release
rmdir /S /Q aval\obj
