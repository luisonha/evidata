# Infra local — Evidata

## Opción principal: .NET Aspire AppHost

```bash
cd src/Evidata.AppHost
dotnet run
```

Aspire levanta automáticamente: PostgreSQL (Docker), Azurite (Blob + Queues), Mailpit.
Dashboard: http://localhost:18888

## Opción alternativa: Docker Compose (sin Aspire)

```bash
docker compose -f infra/local/docker-compose.yml up -d
```
