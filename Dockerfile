# Estágio 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas o projeto primeiro para aproveitar cache de camadas do Docker
COPY ["AntigravitySaaS.Api.csproj", "./"]
RUN dotnet restore "AntigravitySaaS.Api.csproj"

# Copia todo o código-fonte e compila
COPY . .
RUN dotnet publish "AntigravitySaaS.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# Estágio 2: Runtime de Produção (Ubuntu Chiseled enxuto e seguro)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS final
WORKDIR /app
EXPOSE 8080

# Usuário não-root nativo do .NET 8
USER $APP_UID 

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AntigravitySaaS.Api.dll"]
