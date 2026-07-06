# Multi-stage build: SDK image compiles, slim ASP.NET runtime image runs.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so dependency layers cache across code-only changes.
COPY Tiketin.sln ./
COPY src/Tiketin.Web/Tiketin.Web.csproj src/Tiketin.Web/
COPY tests/Tiketin.Tests/Tiketin.Tests.csproj tests/Tiketin.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Tiketin.Web -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as the non-root user shipped with the base image.
RUN mkdir -p /app/storage && chown app:app /app/storage
USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    Storage__Root=/app/storage
EXPOSE 8080
VOLUME /app/storage

ENTRYPOINT ["dotnet", "Tiketin.Web.dll"]
