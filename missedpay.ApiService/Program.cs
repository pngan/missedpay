using Microsoft.EntityFrameworkCore;
using missedpay.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Register tenant provider based on configuration
// Development: Uses hard-coded tenant ID
// Production: Can use Header or JWT based on appsettings.json
builder.Services.AddTenantProvider(builder.Configuration);

// Add PostgreSQL with Aspire - this will automatically use the connection string from Aspire
// DbContext pooling is enabled (works because ITenantProvider is not in constructor)
builder.AddNpgsqlDbContext<MissedPayDbContext>("missedpaydb");

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register AkahuService with HttpClient
builder.Services.AddHttpClient<AkahuService>();

// Load Akahu tokens from environment variables
builder.Configuration["Akahu:UserToken"] = Environment.GetEnvironmentVariable("AKAHU_USER_TOKEN") 
    ?? builder.Configuration["Akahu:UserToken"];
builder.Configuration["Akahu:AppToken"] = Environment.GetEnvironmentVariable("AKAHU_APP_TOKEN") 
    ?? builder.Configuration["Akahu:AppToken"];

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply database migrations automatically on startup with retry logic
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MissedPayDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Set a default tenant ID for migration operations
    dbContext.SetTenantId(Guid.Parse("01927b5e-8f3a-7000-8000-000000000000"));
    
    var retryCount = 0;
    var maxRetries = 20;
    var delay = TimeSpan.FromSeconds(2);
    var migrationApplied = false;
    
    while (retryCount < maxRetries && !migrationApplied)
    {
        retryCount++;
        
        try
        {
            logger.LogInformation("Attempting to connect to database and apply migrations (attempt {Attempt}/{MaxRetries})...", retryCount, maxRetries);
            
            // First, test if we can connect to the database
            var canConnect = await dbContext.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                logger.LogWarning("Cannot connect to database yet. Waiting {Delay} seconds before retry...", delay.TotalSeconds);
                await Task.Delay(delay);
                continue;
            }
            
            logger.LogInformation("Database connection successful.");
            
            // Ensure the migrations history table exists before checking for pending migrations
            logger.LogInformation("Ensuring migrations history table exists...");
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                    ""MigrationId"" character varying(150) NOT NULL,
                    ""ProductVersion"" character varying(32) NOT NULL,
                    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                )");
            
            // Check for pending migrations
            logger.LogInformation("Checking for pending migrations...");
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var pendingList = pendingMigrations.ToList();
            
            if (pendingList.Any())
            {
                logger.LogInformation("Found {Count} pending migration(s): {Migrations}", 
                    pendingList.Count, 
                    string.Join(", ", pendingList));
            }
            else
            {
                logger.LogInformation("No pending migrations found. Database is up to date.");
            }
            
            // Apply migrations
            logger.LogInformation("Applying migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
            
            // Verify tables exist
            var accountCount = await dbContext.Accounts.CountAsync();
            var transactionCount = await dbContext.Transactions.CountAsync();
            logger.LogInformation("Database ready. Current counts - Accounts: {AccountCount}, Transactions: {TransactionCount}", 
                accountCount, transactionCount);
            
            migrationApplied = true;
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P07") // Table already exists
        {
            logger.LogWarning("Tables already exist (SqlState: 42P07). This may be from EnsureCreated or a partial migration. Marking migration as complete...");
            
            try
            {
                // Manually mark the migration as applied since tables already exist
                await dbContext.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20251004083835_MissedPayInitialCreate', '9.0.0') ON CONFLICT DO NOTHING");
                
                logger.LogInformation("Migration marked as complete. Verifying database...");
                
                // Verify tables exist
                var accountCount = await dbContext.Accounts.CountAsync();
                var transactionCount = await dbContext.Transactions.CountAsync();
                logger.LogInformation("Database ready. Current counts - Accounts: {AccountCount}, Transactions: {TransactionCount}", 
                    accountCount, transactionCount);
                
                migrationApplied = true;
            }
            catch (Exception markEx)
            {
                logger.LogError(markEx, "Failed to mark migration as complete. Will retry...");
                
                if (retryCount >= maxRetries)
                {
                    throw;
                }
                
                await Task.Delay(delay);
            }
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P01") // Undefined table
        {
            logger.LogWarning("Migration history table doesn't exist yet (attempt {Attempt}/{MaxRetries}). This is normal on first run. Retrying...", 
                retryCount, maxRetries);
            
            if (retryCount >= maxRetries)
            {
                logger.LogError(pgEx, "Failed to apply database migrations after {MaxRetries} attempts.", maxRetries);
                throw;
            }
            
            await Task.Delay(delay);
        }
        catch (Npgsql.PostgresException pgEx)
        {
            logger.LogWarning(pgEx, "PostgreSQL error (attempt {Attempt}/{MaxRetries}): {Message} (SqlState: {SqlState}). Retrying in {Delay} seconds...", 
                retryCount, maxRetries, pgEx.Message, pgEx.SqlState, delay.TotalSeconds);
            
            if (retryCount >= maxRetries)
            {
                logger.LogError(pgEx, "Failed to apply database migrations after {MaxRetries} attempts. PostgreSQL error: {SqlState}", 
                    maxRetries, pgEx.SqlState);
                throw;
            }
            
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error applying migrations (attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {Delay} seconds...", 
                retryCount, maxRetries, ex.Message, delay.TotalSeconds);
            
            if (retryCount >= maxRetries)
            {
                logger.LogError(ex, "Failed to apply database migrations after {MaxRetries} attempts.", maxRetries);
                throw;
            }
            
            await Task.Delay(delay);
        }
    }
    
    if (!migrationApplied)
    {
        var errorMessage = $"Failed to apply database migrations after {maxRetries} attempts.";
        logger.LogCritical(errorMessage);
        throw new InvalidOperationException(errorMessage);
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Enable CORS
app.UseCors("AllowFrontend");

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
