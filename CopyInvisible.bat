@echo off
echo. 
echo.
echo Build using ReleaseInv and then: 
echo Copy winCronInv (Ctrl-C to cancel)
echo **********************************
pause

echo on

Copy Dist\winCron.exe Dist\winCronInv.exe
Copy Dist\winCron.exe.config Dist\winCronInv.exe.config

@echo off
echo. 
echo. 

pause