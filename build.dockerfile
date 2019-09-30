FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-alpine3.9 AS build

# Install Dotnet core 2.1 too
RUN wget -O dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Sdk/2.1.802/dotnet-sdk-2.1.802-linux-musl-x64.tar.gz \
    && dotnet_sha512='69fac356dd7ee7445e640326a6eedfe95d93d901437fdb6f30de80cb23274ea645cf172d656e72e5a11be5ebd8022a8d9ef7931e5de59d7521331fcbf51b7c15' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -C /usr/share/dotnet -xzf dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet.tar.gz

WORKDIR /repo

