# RTEvents local containers

Run `docker compose up` from the repository root. Compose starts SQL Server,
then the API applies EF Core migrations on startup before serving requests. The
API is available at `http://localhost:8080`, with the Swagger page at
`http://localhost:8080/swagger` and the OpenAPI document at
`http://localhost:8080/openapi/v1.json`.

SQL Server is exposed on port 1433, and data is stored in the `sqlserver-data`
Docker volume. The `sa` password is `RtEvents_Local_2026!` in `compose.yaml`.

On subsequent runs, the API checks migrations again. EF Core applies only
migrations that have not yet been recorded in the database.
Use `docker compose down` to stop the services; the database volume remains.
