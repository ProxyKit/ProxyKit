@ECHO OFF

docker run --rm -it --name proxykit-build ^
 -v %cd%:/repo ^
 -w /repo ^
 -e FEEDZ_PROXYKIT_API_KEY=%FEEDZ_PROXYKIT_API_KEY% ^
 -e BUILD_NUMBER=%GITHUB_RUN_NUMBER% ^
 damianh/dotnet-core-lts-sdks:1 ^
 dotnet run -p build/build.csproj -c Release -- %*

if errorlevel 1 (
  echo Docker build failed: Exit code is %errorlevel%
  exit /b %errorlevel%
)