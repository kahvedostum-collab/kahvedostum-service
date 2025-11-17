FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["KahveDostum_Service/KahveDostum_Service.csproj", "KahveDostum_Service/"]
RUN dotnet restore "KahveDostum_Service/KahveDostum_Service.csproj"
COPY . .
WORKDIR "/src/KahveDostum_Service"
RUN dotnet build "KahveDostum_Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "KahveDostum_Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KahveDostum_Service.dll"]
