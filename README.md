# OrderService

API REST para gestão de pedidos construída com **.NET 8**, **Clean Architecture**, **DDD** e **CQRS leve**.

---

## Sumário

- [Stack](#stack)
- [Pré-requisitos](#pré-requisitos)
- [Como Rodar](#como-rodar)
- [Autenticação](#autenticação)
- [Endpoints](#endpoints)
- [Produtos disponíveis (Seed)](#produtos-disponíveis-seed)
- [Testes](#testes)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Migrations](#migrations)

---

## Stack

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET | 8 | Runtime e SDK |
| ASP.NET Core | 8 | Web API |
| Entity Framework Core | 8 | ORM |
| PostgreSQL | 16 | Banco de dados |
| MediatR | 12 | CQRS / mediator pattern |
| FluentValidation | 11 | Validação de commands |
| xUnit | 2.9 | Testes unitários e de integração |
| FluentAssertions | 7 | Asserções expressivas |
| NSubstitute | 5 | Mocks nos testes unitários |
| Testcontainers | 4 | PostgreSQL real nos integration tests |
| Swashbuckle | 7 | Swagger / OpenAPI |
| Docker Compose | — | Orquestração local |

---

## Pré-requisitos

- [Docker Desktop](https://docs.docker.com/get-docker/) instalado e rodando
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (apenas para rodar os testes localmente)
- [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) para gerar migrations:

```bash
dotnet tool install --global dotnet-ef
```

---

## Como Rodar

### 1. Clonar o repositório

```bash
git clone https://github.com/seu-usuario/OrderService.git
cd OrderService
```

### 2. Configurar variáveis de ambiente

```bash
cp .env.example .env
# Edite .env se quiser trocar senhas ou a JWT secret key
```

### 3. Gerar a Migration inicial

Obrigatório na primeira vez — sem isso o banco não tem tabelas:

```bash
dotnet ef migrations add InitialCreate \
  --project src/OrderService.Infrastructure \
  --startup-project src/OrderService.Api \
  --output-dir Persistence/Migrations
```

### 4. Subir com Docker Compose

```bash
docker compose up --build
```

O comando irá:
1. Subir o PostgreSQL 16
2. Aguardar o healthcheck do banco
3. Buildar e subir a API
4. Aplicar migrations automaticamente no startup
5. Executar seed dos produtos

### Acessar

| Recurso | URL |
|---|---|
| **Swagger UI** | http://localhost:8080 |
| **Health Check** | http://localhost:8080/health |

### Parar e limpar

```bash
# Apenas parar
docker compose down

# Parar e apagar volume do banco (reset completo)
docker compose down -v
```

### Rodar localmente sem Docker

Necessário ter PostgreSQL rodando localmente com as credenciais do `appsettings.Development.json`.

```bash
cd src/OrderService.Api
dotnet run
```

---

## Autenticação

A API usa **JWT Bearer Authentication**.

### 1. Obter token

```http
POST /auth/token
Content-Type: application/json

{
  "username": "admin",
  "password": "admin"
}
```

**Resposta:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer"
}
```

### 2. Usar o token

Adicione o header em todas as requisições:

```
Authorization: Bearer {accessToken}
```

### 3. Autenticar no Swagger

1. Acesse http://localhost:8080
2. Clique em **Authorize** (canto superior direito)
3. Digite `Bearer {seu_token}`
4. Clique em **Authorize**

---

## Endpoints

### Autenticação

| Método | Endpoint | Descrição | Auth |
|---|---|---|---|
| `POST` | `/auth/token` | Gera token JWT | ✗ |

### Pedidos

| Método | Endpoint | Descrição | Auth |
|---|---|---|---|
| `POST` | `/orders` | Cria novo pedido | ✓ |
| `GET` | `/orders/{id}` | Busca pedido por Id | ✓ |
| `GET` | `/orders` | Lista pedidos com filtros e paginação | ✓ |
| `POST` | `/orders/{id}/confirm` | Confirma pedido (idempotente) | ✓ |
| `POST` | `/orders/{id}/cancel` | Cancela pedido (idempotente) | ✓ |

### Health

| Método | Endpoint | Descrição |
|---|---|---|
| `GET` | `/health` | Status da API e do banco |

---

### Exemplos de uso

#### Criar pedido

```http
POST /orders
Authorization: Bearer {token}
Content-Type: application/json

{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "currency": "BRL",
  "items": [
    { "productId": "10000000-0000-0000-0000-000000000001", "quantity": 2 },
    { "productId": "10000000-0000-0000-0000-000000000002", "quantity": 1 }
  ]
}
```

**Resposta 201:**

```json
{
  "id": "a1b2c3d4-...",
  "customerId": "3fa85f64-...",
  "status": "Placed",
  "currency": "BRL",
  "total": 10299.88,
  "items": [
    {
      "productId": "10000000-0000-0000-0000-000000000001",
      "productName": "Notebook Pro 15",
      "unitPrice": 4999.99,
      "quantity": 2,
      "subtotal": 9999.98
    },
    {
      "productId": "10000000-0000-0000-0000-000000000002",
      "productName": "Mouse Gamer RGB",
      "unitPrice": 299.90,
      "quantity": 1,
      "subtotal": 299.90
    }
  ],
  "createdAt": "2025-01-15T10:30:00Z",
  "confirmedAt": null,
  "canceledAt": null
}
```

#### Listar pedidos com filtros

```http
GET /orders?customerId={guid}&status=Placed&from=2025-01-01&to=2025-12-31&page=1&pageSize=20
Authorization: Bearer {token}
```

**Resposta:**

```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

#### Formato de erro

Todos os erros seguem o mesmo envelope:

```json
{
  "status": 400,
  "message": "One or more validation errors occurred.",
  "errors": {
    "Currency": ["Currency must be a 3-letter ISO code (e.g. BRL, USD)."],
    "Items": ["Order must have at least one item."]
  },
  "traceId": "0HN7K2V8QFMAB:00000001"
}
```

| Situação | Status HTTP |
|---|---|
| Validação inválida | `400 Bad Request` |
| Regra de negócio violada | `400 Bad Request` |
| Recurso não encontrado | `404 Not Found` |
| Estoque insuficiente | `409 Conflict` |
| Erro inesperado | `500 Internal Server Error` |

---

## Produtos disponíveis (Seed)

Inseridos automaticamente no primeiro startup:

| Id (resumido) | Nome | Preço | Estoque |
|---|---|---|---|
| `...0001` | Notebook Pro 15 | R$ 4.999,99 | 50 |
| `...0002` | Mouse Gamer RGB | R$ 299,90 | 200 |
| `...0003` | Teclado Mecânico | R$ 599,00 | 150 |
| `...0004` | Monitor 27" 4K | R$ 2.499,00 | 30 |
| `...0005` | Headset Wireless | R$ 799,00 | 80 |
| `...0006` | Webcam Full HD | R$ 349,90 | 120 |
| `...0007` | SSD 1TB NVMe | R$ 599,90 | 100 |
| `...0008` | Cadeira Gamer | R$ 1.899,00 | 25 |

> Id completo: `10000000-0000-0000-0000-00000000000X` onde X é o número da linha.

---

## Testes

### Rodar todos

```bash
dotnet test
```

### Por projeto

```bash
# Unit Tests — sem Docker, rápidos (~ms)
dotnet test tests/OrderService.UnitTests

# Integration Tests — requer Docker rodando
dotnet test tests/OrderService.IntegrationTests
```

### Com cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```
---

## Variáveis de Ambiente

| Variável | Padrão | Descrição |
|---|---|---|
| `POSTGRES_DB` | `orderservice` | Nome do banco |
| `POSTGRES_USER` | `orderuser` | Usuário do banco |
| `POSTGRES_PASSWORD` | `orderpass` | Senha do banco |
| `JWT_SECRET_KEY` | *(ver .env.example)* | Chave JWT — mín. 32 caracteres |
| `JWT_ISSUER` | `OrderService` | Issuer do token |
| `JWT_AUDIENCE` | `OrderServiceClients` | Audience do token |
| `JWT_EXPIRATION_MINUTES` | `60` | Expiração do token em minutos |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Ambiente da aplicação |

---

## Migrations

Aplicadas automaticamente no startup via `Database.MigrateAsync()`.

### Gerar nova migration

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/OrderService.Infrastructure \
  --startup-project src/OrderService.Api \
  --output-dir Persistence/Migrations
```

### Aplicar manualmente

```bash
dotnet ef database update \
  --project src/OrderService.Infrastructure \
  --startup-project src/OrderService.Api
```

### Reverter última migration

```bash
dotnet ef migrations remove \
  --project src/OrderService.Infrastructure \
  --startup-project src/OrderService.Api
```

---

> Para decisões de arquitetura e design, consulte [`docs/decisions.md`](docs/decisions.md).
