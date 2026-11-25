using Reservr.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ReservrDbContext>(connectionName: "reservr");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync();
}

app.MapDefaultEndpoints();
app.MapFeatureEndpoints();

app.Run();
