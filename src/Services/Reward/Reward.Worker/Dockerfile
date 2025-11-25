FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/Shared/Shared.Common/Shared.Common.csproj", "src/Shared/Shared.Common/"]
COPY ["src/Shared/Shared.Messaging/Shared.Messaging.csproj", "src/Shared/Shared.Messaging/"]
COPY ["src/Services/Reward/Reward.Domain/Reward.Domain.csproj", "src/Services/Reward/Reward.Domain/"]
COPY ["src/Services/Reward/Reward.Application/Reward.Application.csproj", "src/Services/Reward/Reward.Application/"]
COPY ["src/Services/Reward/Reward.Infrastructure/Reward.Infrastructure.csproj", "src/Services/Reward/Reward.Infrastructure/"]
COPY ["src/Services/Reward/Reward.Worker/Reward.Worker.csproj", "src/Services/Reward/Reward.Worker/"]

RUN dotnet restore "src/Services/Reward/Reward.Worker/Reward.Worker.csproj"

COPY . .
WORKDIR "/src/src/Services/Reward/Reward.Worker"
RUN dotnet build "Reward.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Reward.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Reward.Worker.dll"]
