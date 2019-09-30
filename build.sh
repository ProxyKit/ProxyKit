#!/usr/bin/env bash

docker build \
 -f build.dockerfile \
 --tag proxykit-build .

docker run --rm --name proxykit-build \
 -v $PWD/artifacts:/repo/artifacts \
 -v $PWD/.git:/repo/.git \
 proxykit-build \
 dotnet run -p /repo/build/build.csproj -c Release -- "$@"