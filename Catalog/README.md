# Catalog – scheletro Clean Architecture su .NET Framework 4.6.2

API REST (ASP.NET Web API 2) di esempio con un dominio a un solo aggregato (`Product`).

## Struttura

```
src/
  Catalog.Domain          entità, invarianti, interfaccia repository        (nessuna dipendenza)
  Catalog.Application     casi d'uso: command/query handler, DTO, eccezioni (-> Domain)
  Catalog.Infrastructure  EF6 DbContext, repository, query, SystemClock    (-> Application)
  Catalog.WebApi          host IIS, controller, composition root Autofac    (-> tutti)
tests/
  Catalog.Domain.Tests        xUnit
  Catalog.Application.Tests   xUnit + Moq
docs/adr/                 decisioni architetturali
```

Le dipendenze puntano verso il centro: il Domain non conosce EF né Web API.

## Avvio

1. Apri `Catalog.sln` con Visual Studio 2019/2022 (con il carico di lavoro *Sviluppo ASP.NET e Web*).
2. Imposta `Catalog.WebApi` come progetto di avvio ed esegui (IIS Express, `https://localhost:44380`).
3. Il database `Catalog` viene creato su `(LocalDb)\MSSQLLocalDB` alla prima richiesta.

| Metodo | URL | Descrizione |
|---|---|---|
| GET | `/api/products?page=1&pageSize=20&includeInactive=false` | lista paginata |
| GET | `/api/products/{id}` | dettaglio |
| POST | `/api/products` `{ "sku", "name", "price" }` | crea → 201 |
| PUT | `/api/products/{id}/price` `{ "price" }` | cambia prezzo → 204 |
| DELETE | `/api/products/{id}` | disattiva (soft delete) → 204 |

Mappatura errori (`ApiExceptionHandler`): `ValidationException` → 400, `NotFoundException` → 404,
`DomainException` → 422, eccezioni non gestite → 500 senza dettagli interni.

## Prossimi passi consigliati

- Sostituire `CreateDatabaseIfNotExists` con le **EF Migrations**.
- Aggiungere la concorrenza ottimistica (`rowversion`).
- Aggiungere decorator Autofac sugli handler per logging, transazioni e validazione.
- Sostituire `TraceExceptionLogger` con Serilog/NLog e aggiungere un correlation id.
- Documentare l'API con Swashbuckle (Swagger) 5.6.x.
