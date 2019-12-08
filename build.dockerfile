FROM mcr.microsoft.com/dotnet/core/sdk:3.1.100-alpine3.10

# Install DotNet Core 2.1
RUN wget -O dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Sdk/2.1.607/dotnet-sdk-2.1.607-linux-musl-x64.tar.gz \
    && mkdir -p /usr/share/dotnet \
    && tar -C /usr/share/dotnet -xzf dotnet.tar.gz \
    && rm dotnet.tar.gz

RUN apk add git

WORKDIR /repo

# Copy slns, csprojs and do a dotnet restore
COPY ./build/*.sln ./build/
COPY ./build/*.csproj ./build/
WORKDIR /repo/build
RUN dotnet restore

WORKDIR /repo
COPY ./*.sln ./
COPY ./src/*/*.csproj ./src/
RUN for file in $(ls src/*.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore

# Copy source files
COPY ./build ./build/
COPY ./src ./src/