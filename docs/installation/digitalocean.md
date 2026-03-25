# Run BaGetter on DigitalOcean

Use [DigitalOcean](https://www.digitalocean.com) to host BaGetter. You can store metadata on a [Managed Database](https://www.digitalocean.com/products/managed-databases) and upload packages to [DigitalOcean Spaces](https://www.digitalocean.com/products/spaces), which is S3-compatible.

## Configure BaGetter

You can modify BaGetter's configurations by editing the `appsettings.json` file or through environment variables. For the full list of configurations, please refer to [BaGetter's configuration](../configuration.md) guide.

### DigitalOcean Spaces

Spaces is S3-compatible. Create a Space and an API key from the [DigitalOcean console](https://cloud.digitalocean.com), then update `appsettings.json`:

```json
{
    ...

    "Storage": {
        "Type": "AwsS3",
        "Endpoint": "https://ams3.digitaloceanspaces.com",
        "Bucket": "nuget-packages",
        "AccessKey": "",
        "SecretKey": ""
    },

    ...
}
```

Replace the endpoint with your region:

| Region | Endpoint |
|---|---|
| New York (NYC3) | `https://nyc3.digitaloceanspaces.com` |
| San Francisco (SFO3) | `https://sfo3.digitaloceanspaces.com` |
| Amsterdam (AMS3) | `https://ams3.digitaloceanspaces.com` |
| Singapore (SGP1) | `https://sgp1.digitaloceanspaces.com` |
| Frankfurt (FRA1) | `https://fra1.digitaloceanspaces.com` |
| Sydney (SYD1) | `https://syd1.digitaloceanspaces.com` |

### Managed Database

# [PostgreSQL](#tab/postgresql)

Create a PostgreSQL managed database cluster, then update `appsettings.json`:

```json
{
    ...

    "Database": {
        "Type": "PostgreSql",
        "ConnectionString": "Host=<host>;Port=25060;Database=bagetter;Username=<user>;Password=<password>;Ssl Mode=Require;Trust Server Certificate=true"
    },

    ...
}
```

# [MySQL](#tab/mysql)

Create a MySQL managed database cluster, then update `appsettings.json`:

```json
{
    ...

    "Database": {
        "Type": "MySql",
        "ConnectionString": "Server=<host>;Port=25060;Database=bagetter;User=<user>;Password=<password>;SslMode=Required"
    },

    ...
}
```

---

## Publish packages

Publish your first package with:

```shell
dotnet nuget push -s http://localhost:5000/v3/index.json package.1.0.0.nupkg
```

Publish your first [symbol package](https://docs.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg) with:

```shell
dotnet nuget push -s http://localhost:5000/v3/index.json symbol.package.1.0.0.snupkg
```

> [!WARNING]
> You should secure your server by requiring an API Key to publish packages. For more information, please refer to the [Require an API Key](../configuration.md#require-an-api-key) guide.

## Restore packages

You can restore packages by using the following package source:

`http://localhost:5000/v3/index.json`

Some helpful guides:

- [Visual Studio](https://docs.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio#package-sources)
- [NuGet.config](https://docs.microsoft.com/en-us/nuget/reference/nuget-config-file#package-source-sections)

## Symbol server

You can load symbols by using the following symbol location:

`http://localhost:5000/api/download/symbols`

For Visual Studio, please refer to the [Configure Debugging](https://docs.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=vs-2017#configure-symbol-locations-and-loading-options) guide.
