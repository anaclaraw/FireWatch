# FireWatch.API Gateway

Gateway central do ecossistema FireWatch. Responsável por autenticação JWT, roteamento de requests para os serviços internos via YARP, rate limiting e CORS.

---

## Visão Geral

O API Gateway é o único ponto de entrada para clientes externos (app mobile, dashboard web, parceiros). Ele valida o token JWT antes de repassar qualquer request para os serviços internos, garantindo que nenhum serviço fique exposto diretamente.

```
Cliente (Mobile / Dashboard / Parceiro)
              ↓
       FireWatch Gateway :5002
              ↓
    ┌─────────────────────┐
    │  Auth Module (JWT)  │  ← POST /auth/login, /register, /refresh
    │  Rate Limiter       │  
    │  YARP Proxy         │  
    └─────────────────────┘
              ↓
   ┌──────────────────────────┐
   │ Data Ingestion  :5000    │  
   │ Risk Analysis   :5001    │  
   └──────────────────────────┘
```

---

## Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| ASP.NET Core | 8.0 | Framework Web |
| YARP | 2.2.0 | Reverse Proxy |
| JWT Bearer | 8.0.8 | Autenticação |
| Entity Framework Core | 8.0.8 | Persistência de usuários |
| Npgsql | 8.0.8 | Driver PostgreSQL |
| BCrypt.Net | 4.0.3 | Hash de senhas |
| FluentValidation | 11.3.0 | Validação de DTOs |
| Swashbuckle (Swagger) | 6.7.3 | Documentação |

---

## Estrutura do Projeto

```
FireWatch.Gateway/
├── Controllers/
│   └── AuthController.cs         # Register, Login, Refresh, Revoke, Me
├── Data/
│   └── GatewayDbContext.cs       # Contexto EF 
├── DTOs/
│   ├── In/
│   │   └── LoginRequest.cs       # LoginRequest, RegisterRequest, RefreshRequest
│   └── Out/
│       └── AuthResponse.cs       
├── Middlewares/
│   ├── GlobalExceptionMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Models/
│   ├── User.cs
│   └── RefreshToken.cs
├── Services/
│   ├── Interfaces/
│   │   └── IAuthService.cs
│   └── AuthService.cs
├── Validators/
│   └── LoginRequestValidator.cs
├── Cors/
│   └── CorsExtensions.cs
├── RateLimiting/
│   └── RateLimitingExtensions.cs
├── appsettings.json
└── Program.cs
```

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/)
- PostgreSQL rodando (compartilhado com o Data Ingestion ou banco separado)

---

## Configuração

### 1. Banco de dados

O Gateway usa um banco PostgreSQL separado para armazenar usuários e refresh tokens. Se o Docker já está rodando do Data Ingestion, só crie o banco:

```bash
docker exec -it firewatch-postgres psql -U postgres -c "CREATE DATABASE firewatch_gateway;"
```

Depois crie as tabelas:

```bash
docker exec -it firewatch-postgres psql -U postgres -d firewatch_gateway
```

```sql
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS users (
    "Id"           UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name"         VARCHAR(150) NOT NULL,
    "Email"        VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" TEXT         NOT NULL,
    "Role"         VARCHAR(30)  NOT NULL DEFAULT 'Viewer',
    "IsActive"     BOOLEAN      NOT NULL DEFAULT TRUE,
    "CreatedAt"    TIMESTAMP    NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS refresh_tokens (
    "Id"        UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "Token"     VARCHAR(500) NOT NULL,
    "ExpiresAt" TIMESTAMP    NOT NULL,
    "IsRevoked" BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP    NOT NULL DEFAULT NOW(),
    "UserId"    UUID         NOT NULL REFERENCES users("Id") ON DELETE CASCADE
);
```

### 2. Configurar `appsettings.json`

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=firewatch_gateway;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "firewatch-super-secret-key-minimo-32-caracteres!!",
    "Issuer": "firewatch-gateway",
    "Audience": "firewatch-clients",
    "ExpiresInMinutes": "60",
    "RefreshExpiresInDays": "7"
  },
  "ReverseProxy": {
    "Routes": {
      "ingestion-route": {
        "ClusterId": "ingestion-cluster",
        "AuthorizationPolicy": "default",
        "Match": { "Path": "/api/ingestion/{**catch-all}" }
      }
    },
    "Clusters": {
      "ingestion-cluster": {
        "Destinations": {
          "ingestion-svc": { "Address": "http://localhost:5000" }
        }
      }
    }
  }
}
```

### 3. Rodar o Gateway

```bash
dotnet run --project FireWatch.Gateway --urls "http://localhost:5002"
```

Swagger disponível em: `http://localhost:5002`

---

## Endpoints de Autenticação

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `POST` | `/auth/register` | Cria novo usuário | Não |
| `POST` | `/auth/login` | Autentica e retorna tokens | Não |
| `POST` | `/auth/refresh` | Renova o access token | Não |
| `POST` | `/auth/revoke` | Revoga refresh token (logout) | Sim |
| `GET` | `/auth/me` | Dados do usuário autenticado | Sim |

### POST `/auth/register`



### POST `/auth/login`



### POST `/auth/refresh`


---

## Roteamento via YARP

O Gateway repassa automaticamente requests autenticadas para os serviços internos por prefixo de rota:

| Rota no Gateway | Serviço de destino | Porta |
|---|---|---|
| `/api/ingestion/*` | Data Ingestion Service | 5000 |
| `/api/risk/*` | Risk Analysis Service | 5001 |

**Todas as rotas do proxy exigem Bearer token válido.**

---

## Rate Limiting

| Política | Limite | Janela | Aplicado em |
|---|---|---|---|
| `default` | 30 requests | 1 minuto | Rotas do proxy |
| `auth` | 10 requests | 1 minuto | Endpoints de auth |

Quando o limite é atingido, retorna `429 Too Many Requests`:

```json
{
  "success": false,
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "message": "Muitas requisições. Tente novamente em alguns instantes."
}
```

---

## Roles de usuário

| Role | Descrição |
|---|---|
| `Viewer` | Apenas leitura (padrão no register) |
| `Analyst` | Pode disparar ingestões |
| `Admin` | Acesso total |

> Roles são atribuídas manualmente no banco por enquanto. 

---

## Tratamento de erros

Todos os erros passam pelo `GlobalExceptionMiddleware` e retornam o formato padrão:

```json
{
  "success": false,
  "errorCode": "UNAUTHORIZED",
  "message": "E-mail ou senha inválidos.",
  "timestamp": "2024-06-01T14:30:00Z"
}
```

| Código HTTP | errorCode | Situação |
|---|---|---|
| 400 | `BAD_REQUEST` | Dados inválidos |
| 401 | `UNAUTHORIZED` | Token inválido ou credenciais erradas |
| 429 | `RATE_LIMIT_EXCEEDED` | Limite de requests atingido |
| 500 | `INTERNAL_ERROR` | Erro interno não esperado |

---

## Fluxo de autenticação

```
1. POST /auth/login → recebe accessToken (60min) + refreshToken (7 dias)
2. Usa accessToken no header: Authorization: Bearer {token}
3. Quando accessToken expirar → POST /auth/refresh com refreshToken
4. Para logout → POST /auth/revoke (revoga o refreshToken)
```

---

## Variáveis de ambiente (produção)

```bash
ConnectionStrings__Postgres="Host=...;Database=firewatch_gateway;..."
Jwt__Secret="chave-jwt-xxxxx-xxxx-xxx-xxxx"
Jwt__Issuer="firewatch-gateway"
Jwt__Audience="firewatch-clients"
Jwt__ExpiresInMinutes="60"
Jwt__RefreshExpiresInDays="7"
```
