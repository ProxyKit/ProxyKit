#!/usr/bin/env bash

set -e

docker run --rm --name proxykit-build \
 -v $PWD:/repo \
 -w /repo \
 -e FEEDZ_PROXYKIT_API_KEY=$FEEDZ_PROXYKIT_API_KEY \
 -e BUILD_NUMBER=$GITHUB_RUN_NUMBER \
 damianh/dotnet-core-lts-sdks \
 dotnet run -p build/build.csproj -c Release -- "$@"