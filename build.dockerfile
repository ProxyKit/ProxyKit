FROM mcr.microsoft.com/dotnet/core/sdk:3.1.200-alpine3.11

# Install DotNet Core 2.1
RUN wget -O dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Sdk/2.1.607/dotnet-sdk-2.1.607-linux-musl-x64.tar.gz \
    && mkdir -p /usr/share/dotnet \
    && tar -C /usr/share/dotnet -xzf dotnet.tar.gz \
    && rm dotnet.tar.gz

RUN apk add git
