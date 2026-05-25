# ─── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copia os arquivos de projeto e restaura dependências (camada cacheável)
COPY OrderService.sln .
COPY src/OrderService.Domain/OrderService.Domain.csproj             src/OrderService.Domain/
COPY src/OrderService.Application/OrderService.Application.csproj   src/OrderService.Application/
COPY src/OrderService.Infrastructure/OrderService.Infrastructure.csproj src/OrderService.Infrastructure/
COPY src/OrderService.Api/OrderService.Api.csproj                   src/OrderService.Api/
COPY tests/OrderService.UnitTests/OrderService.UnitTests.csproj     tests/OrderService.UnitTests/
COPY tests/OrderService.IntegrationTests/OrderService.IntegrationTests.csproj tests/OrderService.IntegrationTests/

RUN dotnet restore

# Copia o restante e faz o publish
# Sem --no-restore: garante que usa apenas os pacotes baixados dentro
# do container, sem referências a caminhos absolutos do host Windows
COPY . .
RUN dotnet publish src/OrderService.Api/OrderService.Api.csproj \
    -c Release \
    -o /app/publish

# ─── Stage 2: Runtime ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Usuário não-root por segurança
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderService.Api.dll"]
