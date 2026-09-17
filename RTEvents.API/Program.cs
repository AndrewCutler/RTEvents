using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("RTEvents")
    ?? throw new InvalidOperationException("Connection string 'RTEvents' is missing.");

builder.Services.AddDbContext<RTEventsDbContext>(opts => opts.UseSqlServer(connectionString));
builder.Services.AddScoped<IEventsService, EventsService>();
builder.Services.AddScoped<ITicketsService, TicketsService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IMessageBus, MessageBus>();
builder.Services.AddHostedService<OutboxHandler>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<RTEventsDbContext>().Database.MigrateAsync();
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

app.UseExceptionHandler("/errors");

app.UseAuthorization();

app.MapControllers();

app.Run();
