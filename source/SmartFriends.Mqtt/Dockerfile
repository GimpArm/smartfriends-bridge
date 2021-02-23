FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["SmartFriends.Mqtt/SmartFriends.Mqtt.csproj", "SmartFriends.Mqtt/"]
COPY ["SmartFriends.Api/SmartFriends.Api.csproj", "SmartFriends.Api/"]
RUN dotnet restore "SmartFriends.Mqtt/SmartFriends.Mqtt.csproj"
COPY . .
WORKDIR "/src/SmartFriends.Mqtt"
RUN dotnet build "SmartFriends.Mqtt.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartFriends.Mqtt.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartFriends.Mqtt.dll"]