@ECHO OFF

docker build ^
 -f build.Dockerfile ^
 --tag proxykit-build .

if errorlevel 1 (
   echo Docker build failed: Exit code is %errorlevel%
   exit /b %errorlevel%
)

docker run --rm --name proxykit-build ^
 -v %cd%/artifacts:/repo/artifacts ^
 -v %cd%/.git:/repo/.git ^
 -e MYGET_API_KEY=$MYGET_API_KEY ^
 proxykit-build ^
 dotnet run -p /repo/build/build.csproj -c Release -- %*

if errorlevel 1 (
   echo Docker build failed: Exit code is %errorlevel%
   exit /b %errorlevel%
)