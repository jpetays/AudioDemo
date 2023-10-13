@echo off
rem
rem caddy run - same as caddy1 behaviour
rem caddy start
rem caddy reload
rem caddy stop
rem
set CADDY_EXE=.\caddy.exe
if not exist "%CADDY_EXE%" (
    echo *
    echo * Caddy executable %CADDY_EXE% not found
    echo * Download it form official site and copy here
    echo *
    goto :eof
)
if not exist "logs" (
    mkdir logs
)
set option=%1
if "%option%" == "" set option=run
if "%option%" == "s" set option=start
if "%option%" == "r" set option=reload
echo %CADDY_EXE% %option%
%CADDY_EXE% %option%
