using Microsoft.EntityFrameworkCore;
using SimpleWalletSystem.Models;
using SimpleWalletSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Database Context with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    
    // Seed initial data
    await SeedData(context);
}

app.Run();

async Task SeedData(AppDbContext context)
{
    // Check if we already have data
    var hasTenants = await context.Tenants.AnyAsync();
    if (!hasTenants)
    {
        Console.WriteLine("Seeding initial data...");
        
        // Add tenants
        var tenants = new[]
        {
            new Tenant { Id = 1, Name = "Fintech Corp" },
            new Tenant { Id = 2, Name = "Marketplace Inc" }
        };
        await context.Tenants.AddRangeAsync(tenants);

        // Add wallets
        var wallets = new[]
        {
            new Wallet { Id = 1, UserId = 101, TenantId = 1, Balance = 1000.00m },
            new Wallet { Id = 2, UserId = 102, TenantId = 1, Balance = 500.00m },
            new Wallet { Id = 3, UserId = 201, TenantId = 2, Balance = 750.00m }
        };
        await context.Wallets.AddRangeAsync(wallets);

        await context.SaveChangesAsync();
        Console.WriteLine("Data seeded successfully!");
    }
    else
    {
        Console.WriteLine("Database already has data.");
    }
}