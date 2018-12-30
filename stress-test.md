Open 3 consoles in this directory.

1. Run the UpstreamServer:

    ```bash
    dotnet run --project .\src\UpstreamServer\UpstreamServer.csproj -c Release
    ```
    
    This will run the upstream server on http://+:5002.

2. Run the SimpleProxy in docker:

    ```bash
    docker build --tag simpleproxy -f ./src/Dockerfile .
    docker run --name simpleproxy -d --rm -p 5001:5001 simpleproxy dotnet run -p ProxyKit/src/SimpleProxy/SimpleProxy.csproj -c Release
    ```
    
    This will run the simple proxy on http://+:5001 that will forward
    requests to http://10.0.75.1:5002.

3. Run nginx in docker:

    ```bash
    docker run --name nginx -d --rm -v $pwd/nginx/nginx.conf:/etc/nginx/nginx.conf -p 5000:5000 nginx
    ```

    This wil run nginx on http://localhost:5000 that will forward requests to
    http://localhost:5002.

4. Using the [WebSurge](https://websurge.west-wind.com/download.aspx), open the
   3 `*.websurge` files and run the tests, tweaking the time and threads
   according to your systems capabilities.