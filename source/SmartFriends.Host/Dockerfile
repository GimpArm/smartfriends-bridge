FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["SmartFriends.Host/SmartFriends.Host.csproj", "SmartFriends.Host/"]
COPY ["SmartFriends.Api/SmartFriends.Api.csproj", "SmartFriends.Api/"]
RUN dotnet restore "SmartFriends.Host/SmartFriends.Host.csproj"
COPY . .
WORKDIR "/src/SmartFriends.Host"
RUN dotnet build "SmartFriends.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartFriends.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartFriends.Host.dll"]