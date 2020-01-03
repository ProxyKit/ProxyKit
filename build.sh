#!/usr/bin/env bash

docker build \
 -f build.dockerfile \
 --tag proxykit-build .

docker run --rm --name proxykit-build \
 -v $PWD:/repo \
 -w /repo \
 -e FEEDZ_PROXYKIT_API_KEY=$FEEDZ_PROXYKIT_API_KEY \
 proxykit-build \
 dotnet run -p build/build.csproj -c Release -- "$@"