# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore SupportRoom.slnx
RUN dotnet publish src/SupportRoom.Api/SupportRoom.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM build AS migrations
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.10
ENTRYPOINT ["/tools/dotnet-ef"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/logs /app/storage \
    && chown -R "$APP_UID:$APP_UID" /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "SupportRoom.Api.dll"]
