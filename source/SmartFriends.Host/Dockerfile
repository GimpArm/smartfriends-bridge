FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base
WORKDIR /app
EXPOSE 80

#Update openssl.conf to allow older obsolete SmartFriends encryption
RUN sed -i '/^\[openssl_init\]/s/$/\nssl_conf = ssl_sect/' /etc/ssl/openssl.cnf && \
	printf "\n[ssl_sect]\nsystem_default = system_default_sect\n\n[system_default_sect]\nMinProtocol = TLSv1.2\nCipherString = DEFAULT@SECLEVEL=2" >> /etc/ssl/openssl.cnf

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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