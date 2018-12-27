Open 3 console windows in this directory.

1. Run the UpstreamServer:

    ```bash
    dotnet run --project .\UpstreamServer\UpstreamServer.csproj -c Release
    ```
    
    This will run the upstream server on http://localhost:5002.

2. Run the SimpleProxy:

    ```bash
    dotnet run --project .\UpstreamServer\UpstreamServer.csproj -c Release
    ```
    
    This will run the simple proxy on http://localhost:5001 that will forward
    requests to http://localhost:5002.

3. Run nginx for windows (for like-for-like comparisons):

    ```bash
    cd nginx-1.14.2
    .\nginx.exe
    ```

    This wil run nginx on http://localhost:5000 that will forward requests to
    http://localhost:5002.

4. Using the [WebSurge](https://websurge.west-wind.com/download.aspx), open the
   3 `*.websurge` files and run the tests, tweaking the time and threads
   according to your systems capabilities.