@echo off
echo %username%
echo "Let's go!"
REM Wait 3 seconds to avoid simultaneous writing on winCron.log
REM PING localhost -n 3 >NUL
REM (echo %date% %time% [%username%] Let's go!) >> winCron.log
Pause