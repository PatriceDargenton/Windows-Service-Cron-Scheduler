@echo off
echo. 
echo. 
echo winCron dist (Ctrl-C to cancel)
echo *******************************
pause

set "pathExe=%cd%\Dist\winCron.exe"
set "pathExe=%pathExe:\=\\%"
for /f "usebackq delims=" %%a in (`"WMIC DATAFILE WHERE name="%pathExe%" get Version /format:Textvaluelist"`) do (
    for /f "delims=" %%# in ("%%a") do set "%%#"
)
FOR /f "tokens=1,2,3,4 delims=." %%a IN ("%version%") do set "version=%%a.%%b.%%c"
set "zipPath=Bak\winCron%version%.tar.gz"

echo on

if not exist Dist\Bak\ mkdir Dist\Bak\
if exist %zipPath% Del %zipPath%
if not exist Dist\DLL\ mkdir Dist\DLL\
Copy Dist\*.dll Dist\DLL\
Copy Dist\*.*.dll Dist\DLL\
Copy Tasks.txt Dist\
Copy TasksInv.txt Dist\
Copy changelog.md Dist\
Copy README.md Dist\
Copy Go.bat Dist\
Copy Go2.bat Dist\
Copy Install.bat Dist\
Copy Uninstall.bat Dist\
Copy Install_winHome.bat Dist\
Copy Uninstall_winHome.bat Dist\
Copy App.config Dist\winCron.exe.config
Copy App.config Dist\winCronInv.exe.config
cd Dist
tar.exe -z -cf %zipPath% *.exe winCron.exe.config winCronInv.exe.config changelog.md README.md Install.bat Uninstall.bat Install_winHome.bat Uninstall_winHome.bat Go.bat Go2.bat Tasks.txt TasksInv.txt DLL

@echo off
echo. 
echo. 

pause