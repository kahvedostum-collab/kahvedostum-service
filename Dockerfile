# 1. Runtime imajı
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 2. Build imajı
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve restore yap
COPY ["KahveDostum_Service/KahveDostum_Service.csproj", "KahveDostum_Service/"]
RUN dotnet restore "KahveDostum_Service/KahveDostum_Service.csproj"

# Tüm solution'u kopyala ve publish et
COPY . .
WORKDIR "/src/KahveDostum_Service"
RUN dotnet publish "KahveDostum_Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 3. Final imaj
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KahveDostum_Service.dll"]
