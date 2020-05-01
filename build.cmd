@ECHO OFF

docker run --rm -it --name proxykit-build ^
 -v %cd%:/repo ^
 -w /repo ^
 -e FEEDZ_PROXYKIT_API_KEY=%FEEDZ_PROXYKIT_API_KEY% ^
 damianh/dotnet-core-lts-sdks ^
 dotnet run -p build/build.csproj -c Release -- %*

if errorlevel 1 (
  echo Docker build failed: Exit code is %errorlevel%
  exit /b %errorlevel%
)