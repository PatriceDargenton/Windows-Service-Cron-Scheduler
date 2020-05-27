sc stop winCron
REM sc stop winCronInv
REM Relative path for Windows Desktop version:
REM %windir%\Microsoft.NET\Framework\v4.0.30319\installutil -u winCron.exe
REM Absolute path for Windows Home version:
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil -u C:\winCron\Dist\winCron.exe
REM %windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil -u C:\winCron\Dist\winCronInv.exe
pause