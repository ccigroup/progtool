set mypath=%cd%
@echo %mypath%
start call %mypath\..\core.bat 0 p1
echo hello
sleep 5
exit
