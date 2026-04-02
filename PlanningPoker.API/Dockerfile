# .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyasýný bulup kopyalýyoruz
COPY ["PlanningPoker.API/PlanningPoker.API.csproj", "PlanningPoker.API/"]
RUN dotnet restore "PlanningPoker.API/PlanningPoker.API.csproj"

# Tüm dosyalarý kopyalayýp derliyoruz
COPY . .
WORKDIR "/src/PlanningPoker.API"
RUN dotnet publish "PlanningPoker.API.csproj" -c Release -o /app/publish

# Çalýþtýrma aþamasý
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PlanningPoker.API.dll"]