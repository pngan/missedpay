using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add PostgreSQL with Aspire - this will automatically use the connection string from Aspire
builder.AddNpgsqlDbContext<MissedPayDbContext>("missedpaydb");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply database migrations automatically on startup with retry logic
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MissedPayDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var retryCount = 0;
    var maxRetries = 15;
    var delay = TimeSpan.FromSeconds(3);
    
    while (retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting to apply database migrations (attempt {Attempt}/{MaxRetries})...", retryCount + 1, maxRetries);
            
            // First, ensure we can connect to the database
            await dbContext.Database.CanConnectAsync();
            logger.LogInformation("Database connection successful.");
            
            // Then apply migrations
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
            break;
        }
        catch (Exception ex) when (retryCount < maxRetries - 1)
        {
            retryCount++;
            logger.LogWarning(ex, "Failed to apply migrations. Retrying in {Delay} seconds... (attempt {Attempt}/{MaxRetries})", delay.TotalSeconds, retryCount, maxRetries);
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations after {MaxRetries} attempts.", maxRetries);
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
