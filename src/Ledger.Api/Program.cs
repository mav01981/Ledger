using Ledger.Domain;
using Ledger.Infrastructure;
using Ledger.Infrastructure.Data;
using Ledger.Infrastructure.EventStore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
