// prompt-007 & prompt-008: Program.cs
// ASP.NET Core 8 – Razor Pages + REST API with Swagger

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Connection string ────────────────────────────────────────────────────────
// Production: managed identity (AZURE_CLIENT_ID env var drives the User Id)
// Local dev:  "Authentication=Active Directory Default" requires az login
var sqlServerFqdn = Environment.GetEnvironmentVariable("SQL_SERVER_FQDN") ?? "";
var databaseName  = Environment.GetEnvironmentVariable("SQL_DATABASE")    ?? "Northwind";
var clientId      = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "";

string connectionString;

// Allow override from appsettings / environment
var configConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(configConn))
{
    connectionString = configConn;
}
else if (!string.IsNullOrWhiteSpace(sqlServerFqdn))
{
    if (!string.IsNullOrWhiteSpace(clientId))
    {
        // User-assigned managed identity (production)
        connectionString =
            $"Server=tcp:{sqlServerFqdn};" +
            $"Database={databaseName};" +
            $"Authentication=Active Directory Managed Identity;" +
            $"User Id={clientId};";
    }
    else
    {
        // Local dev – uses az login credentials
        connectionString =
            $"Server=tcp:{sqlServerFqdn};" +
            $"Database={databaseName};" +
            $"Authentication=Active Directory Default;";
    }
}
else
{
    connectionString = "";   // no DB configured – dummy data mode
}

builder.Services.AddSingleton<string>(connectionString);   // for DI into data layer

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Repository – shared across all pages and controllers
builder.Services.AddScoped<ExpenseManagement.Data.ExpenseRepository>(sp =>
    new ExpenseManagement.Data.ExpenseRepository(
        connectionString,
        sp.GetRequiredService<ILogger<ExpenseManagement.Data.ExpenseRepository>>()
    ));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Expense Management API",
        Version     = "v1",
        Description = "REST API for the Expense Management System",
    });
    // Include XML comments if present
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Swagger UI always available (useful for demos)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Management API v1");
    c.RoutePrefix = "swagger";
});

app.MapRazorPages();
app.MapControllers();

app.Run();
