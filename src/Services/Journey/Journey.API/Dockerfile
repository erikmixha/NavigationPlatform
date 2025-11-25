FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

COPY ["src/Shared/Shared.Common/Shared.Common.csproj", "src/Shared/Shared.Common/"]
COPY ["src/Shared/Shared.Messaging/Shared.Messaging.csproj", "src/Shared/Shared.Messaging/"]
COPY ["src/Services/Journey/Journey.Domain/Journey.Domain.csproj", "src/Services/Journey/Journey.Domain/"]
COPY ["src/Services/Journey/Journey.Application/Journey.Application.csproj", "src/Services/Journey/Journey.Application/"]
COPY ["src/Services/Journey/Journey.Infrastructure/Journey.Infrastructure.csproj", "src/Services/Journey/Journey.Infrastructure/"]
COPY ["src/Services/Journey/Journey.API/Journey.API.csproj", "src/Services/Journey/Journey.API/"]

RUN dotnet restore "src/Services/Journey/Journey.API/Journey.API.csproj"

COPY . .
WORKDIR "/src/src/Services/Journey/Journey.API"
RUN dotnet build "Journey.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Journey.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Journey.API.dll"]

