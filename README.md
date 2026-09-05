# RMS Backend (SylviaNG.Asset)

.NET 10 / ASP.NET Core / EF Core 10 / PostgreSQL backend for the Requisition
Management System (RMS).

**Full setup, running instructions, seeded accounts, and project overview: see the
root `README.md` one level up** (`../README.md`), alongside `../RMS_Database.sql`
for the database.

Quick start:

```bash
dotnet run --launch-profile http
```

Runs on `http://localhost:5112`; Swagger at `/swagger`.

> Historical note: this project was originally scaffolded as a generic "Asset"
> microservice within a larger HRMS template (hence the `SylviaNG.Asset` project
> name and `Asset` entity) before being built out into the RMS application. The
> `Asset` entity itself is not part of RMS's own 13-feature scope.
