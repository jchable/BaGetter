ARG Version=1.0.0

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG Version
ARG TARGETARCH
WORKDIR /src

## Restore layer — only copy project files for better cache reuse
COPY ./Directory.Packages.props ./nuget.config ./src/**/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore BaGetter/BaGetter.csproj --arch $TARGETARCH

## Publish (implicitly builds)
FROM build AS publish
ARG Version
COPY /src .
RUN dotnet publish BaGetter \
    --configuration Release \
    --output /app \
    --no-restore \
    -p Version=${Version} \
    -p DebugType=none \
    -p DebugSymbols=false \
    -p GenerateDocumentationFile=false \
    -p UseAppHost=false \
    -a $TARGETARCH

## Final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base

# Install ICU and timezone data for globalization support
RUN apk add --no-cache icu-libs icu-data-full tzdata \
    && addgroup -S bagetter \
    && adduser -S bagetter -G bagetter

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    Storage__Path="/data" \
    Search__Type="Database" \
    Database__Type="Sqlite" \
    Database__ConnectionString="Data Source=/data/db/bagetter.db"

LABEL org.opencontainers.image.source="https://github.com/bagetter/BaGetter"

# Create data directories with correct ownership
RUN mkdir -p /data/packages /data/symbols /data/db \
    && chown -R bagetter:bagetter /data

WORKDIR /app
COPY --from=publish /app .
RUN chown -R bagetter:bagetter /app

USER bagetter

ENTRYPOINT ["dotnet", "BaGetter.dll"]
