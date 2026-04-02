# .NET 8 SDK kullanarak projeyi derliyoruz
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Proje dosyalarýný kopyalayýp yayýnlýyoruz (Publish)
COPY . ./
RUN dotnet publish -c Release -o out

# Sadece çalýþtýrýlabilir dosyalarý alýp hafif bir sunucuya koyuyoruz
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Uygulamanýn giriþ noktasý (Dosya ismindeki noktalara dikkat!)
ENTRYPOINT ["dotnet", "PlanningPoker.API.dll"]