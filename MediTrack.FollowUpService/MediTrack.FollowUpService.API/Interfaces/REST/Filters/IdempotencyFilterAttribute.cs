using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Filters;

/// <summary>
/// Si el request trae un header Idempotency-Key ya visto para este endpoint,
/// repite la respuesta guardada en vez de re-ejecutar la acción (evita duplicar
/// el registro de cumplimiento ante un reintento del cliente). El header es
/// opcional: sin él, el endpoint funciona exactamente igual que antes.
/// </summary>
public class IdempotencyFilterAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!request.Headers.TryGetValue("Idempotency-Key", out var keyValues) || string.IsNullOrWhiteSpace(keyValues))
        {
            await next();
            return;
        }

        var key = keyValues.ToString();
        var endpoint = $"{request.Method} {request.Path}";
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<FollowUpDbContext>();

        var existing = await dbContext.IdempotencyRecords.FindAsync(key);
        if (existing is not null && existing.Endpoint == endpoint)
        {
            context.Result = new ContentResult
            {
                StatusCode = existing.ResponseStatusCode,
                Content = existing.ResponseBody,
                ContentType = "application/json"
            };
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult { StatusCode: null or >= 200 and < 300 } objectResult)
        {
            dbContext.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Key = key,
                Endpoint = endpoint,
                ResponseStatusCode = objectResult.StatusCode ?? 200,
                ResponseBody = JsonSerializer.Serialize(objectResult.Value),
                CreatedAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }
    }
}
