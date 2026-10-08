# ADR 0001 – Clean Architecture con CQRS "light"

- **Stato:** accettata
- **Contesto:** applicazione .NET Framework 4.6.2 che deve restare testabile e migrabile verso .NET moderno.

## Decisione

1. **Quattro livelli** (Domain, Application, Infrastructure, WebApi) con dipendenze verso il centro.
2. **Command e query handler espliciti** (`ICommandHandler<>`, `IQueryHandler<,>`) invece di MediatR:
   nessuna dipendenza esterna nell'Application e pieno supporto di net462.
3. **Scrittura** tramite aggregate + repository + `IUnitOfWork` (il DbContext EF6).
   **Lettura** tramite `IProductQueries` con `AsNoTracking` e proiezione diretta su DTO.
4. **Autofac** (4.9.x, l'ultima linea compatibile con net462 + Web API 2) e composition root
   solo nel progetto WebApi; un DbContext per richiesta.
5. Domain, Application e Infrastructure usano il **formato csproj SDK-style**: si possono
   multi-targettare (`net462;net8.0`) quando inizierà la migrazione.

## Conseguenze

- Migrare a ASP.NET Core significa riscrivere solo il progetto WebApi (controller e DI).
- Un po' di codice in più (command e handler separati) in cambio di confini chiari e test senza database.
