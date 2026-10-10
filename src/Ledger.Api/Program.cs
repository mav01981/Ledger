using Ledger.Domain;
using Ledger.Infrastructure;
using Ledger.Infrastructure.Data;
using Ledger.Infrastructure.EventStore;
using Ledger.Infrastructure.Outbox;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
            ?? builder.Environment.ApplicationName,
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddNpgsqlInstrumentation()
        .AddMeter(OutboxMetrics.MeterName))
    .UseOtlpExporter();

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(Ledger.Application.Features.OpenAccount.OpenAccountCommand).Assembly));

// Infrastructure (EF Core, event store, outbox, projections)
builder.Services.AddLedgerInfrastructure(builder.Configuration);

// Application services
builder.Services.AddScoped<IEventStore, PostgresEventStore>();
builder.Services.AddScoped<Ledger.Infrastructure.ReadModel.PostgresReadModel>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created (for demo purposes)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.Run();
