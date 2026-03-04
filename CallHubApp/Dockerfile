# -----------------------------
# 1. Build aşaması
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY CallHubApp/*.csproj ./CallHubApp/
RUN dotnet restore

COPY . .
WORKDIR /src/CallHubApp
RUN dotnet publish -c Release -o /app/publish

# -----------------------------
# 2. Runtime aşaması
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Hem web arayüzü (3000) hem socket (5001) portunu aç
EXPOSE 5199 3000

# ASP.NET Core'u her iki portta çalıştır
ENV ASPNETCORE_URLS=http://+:5199;http://+:3000

ENTRYPOINT ["dotnet", "CallHubApp.dll"]
