# === Сборка ===
FROM ghcr.io/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY FreelancePlatform.csproj .
RUN dotnet restore FreelancePlatform.csproj

COPY . .

RUN dotnet publish FreelancePlatform.csproj -c Release -o out

# === Запуск ===
FROM ghcr.io/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "FreelancePlatform.dll"]