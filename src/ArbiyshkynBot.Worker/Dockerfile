# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем файлы проекта и восстанавливаем зависимости
COPY ["ArbiyshkynBot.sln", "./"]
COPY ["src/ArbiyshkynBot.Core/ArbiyshkynBot.Core.csproj", "src/ArbiyshkynBot.Core/"]
COPY ["src/ArbiyshkynBot.Infrastructure/ArbiyshkynBot.Infrastructure.csproj", "src/ArbiyshkynBot.Infrastructure/"]
COPY ["src/ArbiyshkynBot.Worker/ArbiyshkynBot.Worker.csproj", "src/ArbiyshkynBot.Worker/"]

RUN dotnet restore "src/ArbiyshkynBot.Worker/ArbiyshkynBot.Worker.csproj"

# Копируем остальной исходный код и компилируем Release
COPY . .
WORKDIR "/src/src/ArbiyshkynBot.Worker"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Установка зависимостей для Playwright (при необходимости браузерной автоматизации)
RUN apt-get update && apt-get install -y curl libicu-dev libssl-dev libglib2.0-0 libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 libdbus-1-3 libexpat1 libxcb1 libx11-xcb1 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgbm1 libpango-1.0-0 libcairo2 libasound2

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ArbiyshkynBot.Worker.dll"]