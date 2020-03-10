set /p msbuildpath=<msbuildpath.txt
call %msbuildpath% .\CommandRunner\CommandRunner\CommandRunner.csproj /t:Rebuild /p:Configuration=Release
call %msbuildpath% /t:Rebuild /p:Configuration=Release