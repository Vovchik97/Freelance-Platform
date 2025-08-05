# === Сборка ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем .csproj и восстанавливаем зависимости
COPY FreelancePlatform.csproj .
RUN dotnet restore

# Копируем весь код
COPY . .

# Публикуем в папку out
RUN dotnet publish -c Release -o out

# === Запуск ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Копируем опубликованный код
COPY --from=build /app/out .

# Открываем порт
EXPOSE 8080

# Запускаем приложение
ENTRYPOINT ["dotnet", "FreelancePlatform.dll"]