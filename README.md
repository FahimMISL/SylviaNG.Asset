# SylviaNG Asset Microservice

## Overview

The Asset microservice is part of the SylviaNG HRMS ecosystem, responsible for
managing the organization's physical assets — company equipment (IT hardware,
furniture, vehicles, etc.). It tracks each asset's code, category, status,
assignment to an employee, and value. It ships one aggregate root — `Asset`.

> **Naming convention** (matches `SylviaNG.Benefit`): the project/folder/assembly
> is **singular** (`SylviaNG.Asset`, `asset_db`), but all C# **namespaces are
> plural** (`SylviaNG.Assets.*`). This avoids the C# namespace/type collision that
> would otherwise occur between the `Asset` entity type and a `SylviaNG.Asset`
> root namespace.

## Technology Stack

- .NET 10.0
- Entity Framework Core 10.0
- PostgreSQL / SQL Server / Oracle (configurable via `Database:Provider`)
- Keycloak Authentication (JWT)
- Apache Kafka for event-driven architecture (employee sync)
- Finbuckle.MultiTenant for multi-tenancy support
- MediatR for CQRS pattern
- FluentValidation for input validation
- **Manual object mapping** (static mapper extensions — no AutoMapper)
- gRPC for inter-service communication

## Project Structure

```
SylviaNG.Asset/
├── Application/                        # Application layer (business logic, CQRS handlers)
│   ├── Common/
│   │   ├── Exceptions/                # Custom exceptions (NotFoundException, DuplicateException)
│   │   └── Models/                    # Shared DTOs (CoreGrpcModels)
│   ├── Extensions/                     # Authentication/Authorization/DI extensions, ValidationBehavior
│   ├── Features/
│   │   └── Assets/                    # Feature module (follow this pattern for new features)
│   │       ├── Commands/              # CQRS Commands (Create, Update, Delete + Handlers + Validators)
│   │       ├── Models/                # DTOs (Request/Response models)
│   │       └── Queries/               # CQRS Queries (GetAll, GetById, GetPaged + Handlers)
│   ├── Interfaces/                     # Externals, Repositories (IAssetRepository), Services (IAssetService)
│   ├── Mappings/                      # Manual mapper extension classes (AssetMapper)
│   └── Services/                      # Business logic service implementations
├── Domain/                            # Entities (Asset, Employee), Enums, Events
├── Infrastructure/                    # Configurations, Data (ApplicationDBContext), Repositories, Kafka, gRPC
├── Controllers/                       # AssetController
├── Middlewares/                       # ResponseWrapping, GlobalExceptionHandler
├── SharedKernel/                      # Audit, Generic repo + UoW, Pagination, Utils
├── Protos/                            # core.proto
├── Migrations/                        # EF Core migrations
├── Program.cs
├── appsettings.json
└── Dockerfile

SylviaNG.Asset.Tests/                  # Controllers / Services / Validators tests
```

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL / SQL Server / Oracle database
- Keycloak instance for authentication
- Apache Kafka (for employee sync events)

### Configuration

```json
{
  "Database": {
    "Provider": "Postgresql",
    "ConnectionString": "Host=localhost;Port=5432;Database=asset_db;Username=user;Password=pass"
  },
  "Keycloak": {
    "Authority": "http://localhost:8082/realms/sylviang",
    "ClientId": "sylviang-api",
    "ClientSecret": "your-client-secret"
  }
}
```

### Running the Service

```bash
cd SylviaNG.Asset
dotnet restore
dotnet run
```

The service will start on:

- HTTP: http://localhost:5211
- HTTPS: https://localhost:7211

Swagger UI: `http://localhost:5211/swagger`

## Features

- Multi-tenant support via JWT claims (`tenant_id`)
- Clean Architecture with CQRS (MediatR) + Repository + Unit of Work
- Global exception handling + response wrapping (`{ hasError, decentMessage, errorDetails, content }`)
- UTC DateTime enforcement via EF interceptor; audit base entity
- **Manual object mapping** (static mapper extensions — no AutoMapper)
- Kafka employee sync + gRPC inter-service communication

## Database Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

> The template's `Employee` entity carries model-only navigation properties, so the
> first `migrations add` may bundle extra operations — that is expected, not a bug.

## Testing

```bash
cd SylviaNG.Asset.Tests
dotnet test
```

## How to Add a New Feature

Follow the existing `Assets` pattern:

1. **Domain** — entity in `Domain/Entities/` inheriting from `Audit`
2. **Infrastructure** — `DbSet` in `ApplicationDBContext`, configuration in `Configurations/`, repository in `Repositories/`
3. **Application** — feature folder in `Features/` with `Commands/`, `Queries/`, `Models/`
4. **Mappings** — a **manual** mapper class in `Mappings/` (`ToEntity`, `ApplyUpdate`, `ToResponse`, `ToLookupResponse`)
5. **Services** — interface in `Interfaces/Services/`, implementation in `Services/`
6. **DI** — register repository in `Infrastructure/Extensions/DependencyInjection.cs` and service in `Application/Extensions/DependencyInjection.cs`
7. **Controller** — in `Controllers/` using MediatR for CQRS
8. **Tests** — service/controller/validator tests in `Tests/`

## Related Projects

- **SylviaNG.Recruitment** — canonical copy-source
- **SylviaNG.Community** — community/announcements microservice
- **SylviaNG.Cafeteria** — cafeteria management microservice