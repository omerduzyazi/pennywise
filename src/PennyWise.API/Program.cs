using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PennyWise.Infrastructure;
using PennyWise.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Service Registration ─────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PennyWise API",
        Version = "v1",
        Description = "Personal Finance & Budget Tracking API"
    });

    // Add JWT Bearer auth to Swagger UI
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register Infrastructure (EF Core + PostgreSQL + Repositories + JWT)
builder.Services.AddInfrastructure(builder.Configuration);

// ── JWT Authentication ──────────────────────────────────────────
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS — allow frontend origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate on startup (Development only) ──────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PennyWiseDbContext>();

    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }

    if (!db.Users.Any())
    {
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Add Admin
        db.Users.Add(new PennyWise.Domain.Entities.User
        {
            Id = adminId,
            Email = "admin@pennywise.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FullName = "Sistem Yöneticisi",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        });
        
        // Add Normal User
        db.Users.Add(new PennyWise.Domain.Entities.User
        {
            Id = userId,
            Email = "demo@pennywise.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            FullName = "Demo Kullanıcı",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        });

        // Add Demo Portfolios
        var portfolioId = Guid.NewGuid();
        db.Portfolios.Add(new PennyWise.Domain.Entities.Portfolio
        {
            Id = portfolioId,
            UserId = userId,
            Name = "Teknoloji Fonum",
            CreatedAt = DateTime.UtcNow
        });

        // Add Holdings
        db.Holdings.Add(new PennyWise.Domain.Entities.Holding
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            TickerSymbol = "AAPL",
            InstrumentName = "Apple Inc.",
            InstrumentType = PennyWise.Domain.Enums.InstrumentType.Stock,
            PurchasePrice = 150.0m,
            Quantity = 10m,
            CurrentPrice = 185.5m,
            PurchaseDate = DateTime.UtcNow.AddMonths(-6),
            CreatedAt = DateTime.UtcNow
        });

        db.Holdings.Add(new PennyWise.Domain.Entities.Holding
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolioId,
            TickerSymbol = "BTC",
            InstrumentName = "Bitcoin",
            InstrumentType = PennyWise.Domain.Enums.InstrumentType.Crypto,
            PurchasePrice = 45000m,
            Quantity = 0.5m,
            CurrentPrice = 65000m,
            PurchaseDate = DateTime.UtcNow.AddMonths(-3),
            CreatedAt = DateTime.UtcNow
        });

        // Add Transactions
        db.Transactions.Add(new PennyWise.Domain.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = PennyWise.Domain.Enums.TransactionType.Income,
            Amount = 45000m,
            Category = "Maaş",
            Description = "Aylık Maaş Ödemesi",
            TransactionDate = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow
        });

        db.Transactions.Add(new PennyWise.Domain.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = PennyWise.Domain.Enums.TransactionType.Expense,
            Amount = 15000m,
            Category = "Kira",
            Description = "Ev Kirası",
            TransactionDate = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });

        db.Transactions.Add(new PennyWise.Domain.Entities.Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = PennyWise.Domain.Enums.TransactionType.Expense,
            Amount = 3500m,
            Category = "Market",
            Description = "Haftalık Alışveriş",
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        // Add Budgets
        db.Budgets.Add(new PennyWise.Domain.Entities.Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = "Market",
            LimitAmount = 10000m,
            Month = DateTime.UtcNow.Month,
            Year = DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}

app.Run();

// Make the implicit Program class public for WebApplicationFactory in tests
public partial class Program { }
