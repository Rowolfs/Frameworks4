using Frameworks4.Services;
using Frameworks4.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Booking API", Version = "v1" });
});

// Singleton-сервисы (состояние в памяти)
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddSingleton<BookingService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<BookingReadinessCheck>("readiness");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Liveness — всегда 200
app.MapHealthChecks("/health/live");

// Readiness — падает при критической деградации (>10 ошибок)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Name == "readiness"
});

app.Run();
