# BaGetter 🥖🛒

BaGetter is a lightweight [NuGet] and [symbol] server, written in C#. It's forked from [BaGet] for progressive and community driven development.

## Getting Started

With Docker:

1. `docker run -p 5000:8080 -v ./bagetter-data:/data bagetter/bagetter:latest`
2. Browse `http://localhost:5000/` in your browser

With .NET:

1. Install the [.NET SDK]
2. Download and extract [BaGetter's latest release]
3. Start the service with `dotnet BaGetter.dll`
4. Browse `http://localhost:5000/` in your browser

With IIS ([official microsoft documentation](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis)):

1. Install the [hosting bundle](https://dotnet.microsoft.com/permalink/dotnetcore-current-windows-runtime-bundle-installer)
2. Download the [zip release](https://github.com/bagetter/BaGetter/releases) of BaGetter
3. Unpack the zip file contents to a folder of your choice
4. Create a new or configure an existing IIS site to point its physical path to the folder where you unpacked the zip file

For more information, please refer to the [documentation].

## Features

* **Cross-platform**: runs on Windows, macOS, and Linux!
* **ARM** (64bit) **support**. You can host your NuGets on a device like Raspberry Pi!
* **Cloud native**: supports [Docker][Docker doc link], [AWS][AWS doc link]
* **Offline support**: [Mirror a NuGet server][Read through caching] to speed up builds and enable offline downloads