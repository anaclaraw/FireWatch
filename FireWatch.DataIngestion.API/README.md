# FireWatch.Data Ingestion Service

Serviço responsável por coletar, normalizar, persistir e publicar dados espaciais de focos de calor no barramento de eventos do FireWatch.

---

## Visão Geral

O Data Ingestion Service é a porta de entrada de dados do ecossistema FireWatch. Ele consome fontes externas de dados espaciais , valida e normaliza os registros, persiste no banco de dados e publica eventos no RabbitMQ para que outros serviços (como o Risk Analysis) possam reagir em tempo real.

```
Fonte Externa (NASA FIRMS)
        ↓
Data Ingestion Service
        ↓
   PostgreSQL (persiste)
        ↓
   RabbitMQ (publica evento SpatialDataReceivedEvent)
        ↓
Risk Analysis Service (consome)
```

---

## Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| ASP.NET Core | 8.0 | Framework Web |
| Entity Framework Core | 8.0.8 | ORM |
| Npgsql | 8.0.8 | Driver PostgreSQL |
| RabbitMQ.Client | latest | Mensageria |
| FluentValidation | 11.3.0 | Validação de DTOs |
| Swashbuckle (Swagger) | 6.7.3 | Documentação da API |

---

## Arquitetura em Camadas

```
FireWatch.DataIngestion/
├── FireWatch.DataIngestion.API/           
│   ├── Controllers/
│   │   └── IngestionController.cs        
│   ├── DTOs/
│   │   ├── SpatialDataRequest.cs         
│   │   ├── BulkIngestionRequest.cs      
│   │   └── IngestionResponse.cs          
│   ├── Validators/
│   │   └── SpatialDataRequestValidator.cs
│   ├── Middlewares/
│   │   └── GlobalExceptionMiddleware.cs
│   └── Program.cs
│
├── FireWatch.DataIngestion.Application/  
│   ├── Services/
│   │   ├── IIngestionService.cs
│   │   └── IngestionService.cs          
│   ├── Interfaces/
│   │   ├── IEventPublisher.cs
│   │   └── IDataSourceClient.cs
│   ├── Events/
│   │   └── SpatialDataReceivedEvent.cs  
│   └── DTOs/
│       └── RawSpatialData.cs
│
├── FireWatch.DataIngestion.Domain/       
│   ├── Entities/
│   │   ├── BaseEntity.cs
│   │   └── SpatialRecord.cs              
│   ├── ValueObjects/
│   │   └── Coordinates.cs               
│   ├── Enums/
│   │   ├── DataSourceType.cs
│   │   └── ProcessingStatus.cs
│   ├── Exceptions/
│   │   └── DomainException.cs
│   └── Interfaces/
│       └── ISpatialRecordRepository.cs
│
└── FireWatch.DataIngestion.Infrastructure/ 
    ├── Clients/
    │   └── FirmsHttpClient.cs            
    ├── Messaging/
    │   └── RabbitMQEventPublisher.cs     
    ├── Persistence/
    │   ├── AppDbContext.cs
    │   └── Configurations/
    │       └── SpatialRecordConfiguration.cs
    ├── Repositories/
    │   └── SpatialRecordRepository.cs
    └── DI/
        └── ServiceCollectionExtensions.cs
```

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/)
- API Key gratuita da [NASA FIRMS](https://firms.modaps.eosdis.nasa.gov/api/)

---

## Configuração

### 1. Subir infraestrutura com Docker

```bash
# PostgreSQL
docker run -d \
  --name firewatch-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=firewatch_ingestion \
  -p 5432:5432 \
  postgres:16

# RabbitMQ com dash
docker run -d \
  --name firewatch-rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```



### 2.  `appsettings.json`

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=firewatch_ingestion;Username=postgres;Password=postgres"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": "5672",
    "User": "guest",
    "Password": "guest"
  },
  "ExternalSources": {
    "NasaFirms": {
      "ApiKey": "API_KEY"
    }
  }
}
```

### 3. Criar banco e aplicar schema

```bash
cd FireWatch.DataIngestion.API
dotnet ef database update  --project ../FireWatch.DataIngestion.Infrastructure
```

### 4. Iniciar serviço

```bash
dotnet run --project FireWatch.DataIngestion.API --urls "http://localhost:5000"
```

Swagger disponível em: `http://localhost:5000/swagger`

---

## Endpoints

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/ingestion/health` | Health check | Não |
| `POST` | `/api/ingestion/single` | Ingere um registro manual | Sim |
| `POST` | `/api/ingestion/bulk` | Ingestão em lote de fonte externa | Sim |
| `GET` | `/api/ingestion` | Lista registros por fonte e período | Sim |
| `GET` | `/api/ingestion/{id}` | Busca registro por ID | Sim |

### POST `/api/ingestion/single`
![login](./single.png)

### POST `/api/ingestion/bulk`
![login](./bulk.png)

### GET `/api/ingestion`
![login](./ingestion.png)


---

## Eventos publicados no RabbitMQ

Após cada ingestão bem-sucedida, o serviço publica no exchange `firewatch.events`:

| Routing Key | Evento | Consumidor |
|---|---|---|
| `firewatch.spatial.received` | `SpatialDataReceivedEvent` | Risk Analysis Service |



---

## Regras de validação

| Campo | Regra |
|---|---|
| `latitude` | Entre -90 e 90 |
| `longitude` | Entre -180 e 180 |
| `brightness` | Maior que 0 |
| `confidence` | Entre 0 e 100 |
| `source` | Deve ser um dos valores aceitos |
| `dayNight` | `D` ou `N` |
| `acquiredAt` | Não pode ser data futura |

---

## Integração NASA FIRMS

O serviço consome o sensor **VIIRS_SNPP_NRT** cobrindo o Brasil:

- **Bbox:** `-73.99, -33.75, -28.85, 5.27`
- **Atualização:** várias vezes ao dia
- **Campos mapeados:** latitude, longitude, brightness (ti4), frp, confidence, acq_date, acq_time, daynight



---

## Variáveis de ambiente 

```bash
ConnectionStrings__Postgres="Host=...;Database=...;Username=...;Password=..."
RabbitMQ__Host="rabbitmq-host"
RabbitMQ__User="usuario"
RabbitMQ__Password="senha"
ExternalSources__NasaFirms__ApiKey="api-key"
```
