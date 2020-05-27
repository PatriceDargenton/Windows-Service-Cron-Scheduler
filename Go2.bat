@echo off
echo %username%
echo "Let's go too!"
REM Wait 3 seconds to avoid simultaneous writing on winCron.log
REM PING localhost -n 3 >NUL
REM (echo %date% %time% [%username%] Let's go too!) >> winCron.log
Pause