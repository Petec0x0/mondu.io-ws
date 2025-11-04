using Microsoft.EntityFrameworkCore;
using SimpleWalletSystem.Models;
using SimpleWalletSystem.Services;
using SimpleWalletSystem.Middleware;

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
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add middleware
app.UseMiddleware<TenantMiddleware>();

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
        
        // Create GUIDs for consistent seeding
        var tenant1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenant2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var user1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var user2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var user3Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        
        // Add tenants
        var tenants = new[]
        {
            new Tenant { Id = tenant1Id, Name = "Fintech Corp" },
            new Tenant { Id = tenant2Id, Name = "Marketplace Inc" }
        };
        await context.Tenants.AddRangeAsync(tenants);

        // Add wallets
        var wallets = new[]
        {
            new Wallet { Id = Guid.NewGuid(), UserId = user1Id, TenantId = tenant1Id, Balance = 1000.00m },
            new Wallet { Id = Guid.NewGuid(), UserId = user2Id, TenantId = tenant1Id, Balance = 500.00m },
            new Wallet { Id = Guid.NewGuid(), UserId = user3Id, TenantId = tenant2Id, Balance = 750.00m }
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