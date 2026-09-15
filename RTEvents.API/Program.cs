using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("RTEvents")
    ?? throw new InvalidOperationException("Connection string 'RTEvents' is missing.");

builder.Services.AddDbContext<RTEventsDbContext>(opts => opts.UseSqlServer(connectionString));

var app = builder.Build();

for (var attempt = 1; ; attempt++)
{
    try
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RTEventsDbContext>()
            .Database.MigrateAsync();
        app.Logger.LogInformation("Database migrations applied.");
        break;
    }
    catch (SqlException ex) when (attempt < 60)
    {
        app.Logger.LogWarning(ex, "SQL Server is not ready (attempt {Attempt}/60).", attempt);
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "RTEvents API v1"));
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
