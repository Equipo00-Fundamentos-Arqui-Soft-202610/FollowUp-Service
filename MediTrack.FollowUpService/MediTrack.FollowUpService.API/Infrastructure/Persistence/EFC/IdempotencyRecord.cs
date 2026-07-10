namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;

/// <summary>
/// Registro de respuestas ya servidas para una Idempotency-Key dada: si el
/// cliente reintenta el mismo request con la misma key, se repite la respuesta
/// guardada en vez de re-ejecutar el comando (evita duplicar registros de
/// cumplimiento y descontar stock dos veces).
/// </summary>
public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
