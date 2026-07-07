# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["global.json", "Directory.Packages.props", "Mci.sln", "./"]
COPY ["src/Mci.Core/Mci.Core.csproj", "src/Mci.Core/"]
COPY ["src/Mci.Infrastructure/Mci.Infrastructure.csproj", "src/Mci.Infrastructure/"]
COPY ["src/Mci.Api/Mci.Api.csproj", "src/Mci.Api/"]

RUN dotnet restore "src/Mci.Api/Mci.Api.csproj"

COPY . .
RUN dotnet publish "src/Mci.Api/Mci.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mci.Api.dll"]
