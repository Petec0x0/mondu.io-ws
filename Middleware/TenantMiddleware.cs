using SimpleWalletSystem.Services;

namespace SimpleWalletSystem.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        // Extract tenant from header (in real scenario, this would come from JWT)
        var tenantHeader = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
        
        if (Guid.TryParse(tenantHeader, out var tenantId))
        {
            tenantService.SetTenant(tenantId);
        }
        else
        {
            // For demo - use default tenant
            tenantService.SetTenant(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        }

        await _next(context);
    }
}

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
}

public class CurrentTenantService : ICurrentTenantService
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        TenantId = tenantId;
    }
}