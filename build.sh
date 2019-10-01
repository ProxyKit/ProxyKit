#!/usr/bin/env bash

docker build \
 --build-arg MYGET_API_KEY=$MYGET_API_KEY \
 -f build.dockerfile \
 --tag proxykit-build .

docker run --rm --name proxykit-build \
 --build-arg MYGET_API_KEY=$MYGET_API_KEY \
 -v $PWD/artifacts:/repo/artifacts \
 -v $PWD/.git:/repo/.git \
 proxykit-build \
 dotnet run -p /repo/build/build.csproj -c Release -- "$@"