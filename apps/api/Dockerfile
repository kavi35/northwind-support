# Built from the repository root:
#   docker build -f apps/api/Dockerfile .
# Restore is its own layer so a later source change does not restore again.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY apps/api/src/SupportDesk.Domain/SupportDesk.Domain.csproj apps/api/src/SupportDesk.Domain/
COPY apps/api/src/SupportDesk.Application/SupportDesk.Application.csproj apps/api/src/SupportDesk.Application/
COPY apps/api/src/SupportDesk.Infrastructure/SupportDesk.Infrastructure.csproj apps/api/src/SupportDesk.Infrastructure/
COPY apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj apps/api/src/SupportDesk.Presentation/

RUN dotnet restore apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj

COPY apps/api/src/ apps/api/src/

RUN dotnet publish apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# HTTP only. The image has no development certificate and does not listen on 8081.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# The ASP.NET runtime image provides this account. whoami prints "app", not "root".
USER app

ENTRYPOINT ["dotnet", "SupportDesk.Presentation.dll"]
